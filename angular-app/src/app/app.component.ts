import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
import { TreeNodeComponent, TreeNode } from './tree-node.component';
import { GraphViewComponent } from './graph-view.component';
import { WebSocketService, TagReadResponse, TagWriteResponse } from './websocket.service';
import { MqttService, AvailableTag } from './mqtt.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, TreeNodeComponent, GraphViewComponent],
  styleUrls: [],
  template: `
    <div class="container">
      <div class="header">
        <h1>🔌 PLC Tag Monitor</h1>
      </div>

      <div class="controls">
        <div class="input-group">
          <label for="tagSelect">Select Tag:</label>
          <select
            id="tagSelect"
            [(ngModel)]="selectedTagName"
            (change)="onTagSelected()"
            [disabled]="loading || loadingTags"
            class="tag-select"
          >
            <option value="">-- Select a tag --</option>
            <option *ngFor="let tag of availableTags" [value]="tag.name">
              {{ tag.name }} ({{ tag.type }})
            </option>
          </select>
        </div>
        <div class="input-group" *ngIf="selectedTagName">
          <label for="autoRefresh">Auto Refresh (5s):</label>
          <input
            id="autoRefresh"
            type="checkbox"
            [(ngModel)]="autoRefreshEnabled"
            (change)="toggleAutoRefresh()"
          />
        </div>
        <div class="signalr-status" [class.connected]="websocketConnected" [class.disconnected]="!websocketConnected">
          <span class="status-dot"></span>
          WebSocket: {{ websocketConnected ? 'Connected' : 'Disconnected' }}
        </div>
      </div>

      <div *ngIf="error" class="error">
        ❌ {{ error }}
      </div>

      <div *ngIf="status" class="status" [class.connected]="isConnected" [class.disconnected]="!isConnected">
        <span class="status-indicator" [class.active]="isConnected" [class.inactive]="!isConnected"></span>
        {{ status }}
      </div>

      <div *ngIf="loading && !treeData" class="loading">
        ⏳ Loading structure...
      </div>

      <div *ngIf="!loading && !treeData && !error && !loadingTags" class="empty-state">
        Select a tag from the dropdown to begin
      </div>
      
      <div *ngIf="loadingTags" class="loading">
        ⏳ Loading available tags...
      </div>

      <div *ngIf="treeData" class="tree-container">
        <app-graph-view 
          [treeData]="treeData"
          (writeValue)="writeValue($event)"
          (editingStart)="onEditingStart($event)"
          (editingEnd)="onEditingEnd($event)"
        ></app-graph-view>
      </div>
    </div>
  `,
  styles: []
})
export class AppComponent implements OnInit, OnDestroy {
  selectedTagName: string = '';
  availableTags: AvailableTag[] = [];
  loadingTags: boolean = false;
  tagName: string = '';
  treeData: TreeNode[] = [];
  loading: boolean = false;
  error: string = '';
  status: string = '';
  isConnected: boolean = false;
  websocketConnected: boolean = false;
  autoRefreshEnabled: boolean = true;
  private updateSubscription?: Subscription;
  private websocketSubscriptions?: Subscription;
  private previousData: any = null;
  private editingPaths: Set<string> = new Set();  // Track which paths are being edited

  constructor(
    private http: HttpClient,
    private websocketService: WebSocketService,
    private mqttService: MqttService
  ) {}

  ngOnInit() {
    this.checkStatus();
    this.loadAvailableTags();
    this.setupWebSocket();
  }

  ngOnDestroy() {
    if (this.updateSubscription) {
      this.updateSubscription.unsubscribe();
    }
    if (this.websocketSubscriptions) {
      this.websocketSubscriptions.unsubscribe();
    }
    if (this.tagName) {
      this.websocketService.unsubscribeFromTag(this.tagName);
    }
    this.websocketService.disconnect();
  }

  loadAvailableTags() {
    this.loadingTags = true;
    this.mqttService.getAvailableTags().subscribe({
      next: (response) => {
        this.availableTags = response.tags;
        this.loadingTags = false;
      },
      error: (err) => {
        console.error('Error loading available tags:', err);
        this.loadingTags = false;
        this.error = 'Error loading available tags';
      }
    });
  }

  onTagSelected() {
    if (this.selectedTagName) {
      // Desuscribirse del tag anterior si existe
      if (this.tagName) {
        this.websocketService.unsubscribeFromTag(this.tagName);
      }
      
      this.tagName = this.selectedTagName;
      
      // Suscribirse al nuevo tag ANTES de cargar la estructura
      this.websocketService.subscribeToTag(this.tagName);
      console.log(`✅ Subscribed to tag, now loading structure for tag: ${this.tagName}`);
      this.loadStructure();
      
      // Iniciar auto-refresh si está habilitado
      if (this.autoRefreshEnabled) {
        this.startAutoUpdate();
      }
    }
  }

  setupWebSocket() {
    // Escuchar cambios de estado de WebSocket
    this.websocketSubscriptions = this.websocketService.connectionState.subscribe(state => {
      this.websocketConnected = state === 'connected';
    });

    // Escuchar respuestas de lectura
    this.websocketSubscriptions.add(
      this.websocketService.tagReadResponse.subscribe((response: TagReadResponse) => {
        console.log('📥 SignalR TagReadResponse received:', response);
        console.log('   Current tagName:', this.tagName);
        console.log('   Response tagName:', response.tagName);
        console.log('   Has error:', response.hasError);
        console.log('   Has value:', !!response.value);
        console.log('   Value type:', typeof response.value);
        
        // Verificar si el tagName coincide (puede ser exacto o puede venir con prefijo/sufijo)
        const tagMatches = response.tagName === this.tagName || 
                          response.tagName.endsWith(this.tagName) ||
                          this.tagName.endsWith(response.tagName);
        
        if (tagMatches && !response.hasError) {
          if (response.value) {
            console.log('✅ Updating tree with response value:', response.value);
            this.loading = false;
            this.error = '';
            this.updateTreeFromResponse(response.value);
          } else {
            console.warn('⚠️ Response has no value');
            this.loading = false;
            this.error = 'Response received but no value data';
          }
        } else if (response.hasError) {
          console.error('❌ Tag read error:', response.errorMessage);
          this.loading = false;
          this.error = response.errorMessage || 'Error reading tag';
        } else {
          console.warn('⚠️ Response tagName mismatch:', {
            current: this.tagName,
            response: response.tagName
          });
        }
      })
    );

    // Escuchar respuestas de escritura
    this.websocketSubscriptions.add(
      this.websocketService.tagWriteResponse.subscribe((response: TagWriteResponse) => {
        if (response.success) {
          console.log('✅ Value written successfully:', response);
          // Refrescar valores después de escribir
          if (this.autoRefreshEnabled) {
            this.sendReadRequest();
          }
        } else {
          this.error = response.errorMessage || 'Error writing value';
        }
      })
    );

    // Verificar estado inicial
    this.websocketConnected = this.websocketService.isConnected();
  }

  toggleAutoRefresh() {
    if (this.autoRefreshEnabled) {
      this.startAutoUpdate();
    } else {
      this.stopAutoUpdate();
    }
  }

  checkStatus() {
    this.http.get<any>('/api/plc/status').subscribe({
      next: (status) => {
        this.isConnected = status.isConnected;
        this.status = `PLC: ${status.state} | IP: ${status.ipAddress}`;
      },
      error: () => {
        this.isConnected = false;
        this.status = 'PLC: No conectado';
      }
    });
  }

  loadStructure() {
    if (!this.tagName.trim()) {
      return;
    }

    this.loading = true;
    this.error = '';
    this.treeData = [];

    // Detener actualizaciones anteriores
    this.stopAutoUpdate();

    // Enviar petición de lectura por MQTT
    this.sendReadRequest();
  }

  sendReadRequest() {
    if (!this.tagName.trim()) {
      return;
    }

    const request = {
      tagName: this.tagName,
      correlationId: `read-${Date.now()}`
    };

    this.mqttService.sendTagReadRequest(request).subscribe({
      next: () => {
        // La respuesta llegará por SignalR
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.error || err.message || 'Error sending read request';
        console.error('Error sending read request:', err);
      }
    });
  }

  updateTreeFromResponse(value: any) {
    console.log('🔄 updateTreeFromResponse called with value:', value);
    console.log('   Current treeData length:', this.treeData?.length || 0);
    
    if (!value) {
      console.warn('⚠️ updateTreeFromResponse: value is null or undefined');
      return;
    }
    
    try {
      if (!this.treeData || this.treeData.length === 0) {
        // Primera carga, construir el árbol completo
        console.log('🌳 Building initial tree structure');
        this.treeData = this.buildTree(value, '');
        this.previousData = this.deepClone(value);
        console.log('✅ Tree built successfully, nodes:', this.treeData.length);
      } else {
        // Actualizar valores existentes
        console.log('🔄 Updating existing tree values');
        this.updateTreeValues(this.treeData, value, this.previousData);
        this.previousData = this.deepClone(value);
        console.log('✅ Tree values updated');
      }
    } catch (error) {
      console.error('❌ Error in updateTreeFromResponse:', error);
      this.error = `Error processing response: ${error}`;
    }
  }

  startAutoUpdate() {
    if (!this.autoRefreshEnabled || !this.tagName) {
      return;
    }

    // Detener actualizaciones anteriores si existen
    this.stopAutoUpdate();

    // Enviar petición cada 5 segundos
    this.updateSubscription = interval(5000).subscribe(() => {
      if (this.tagName && this.autoRefreshEnabled) {
        this.sendReadRequest();
      }
    });

    // Enviar primera petición inmediatamente
    this.sendReadRequest();
  }

  stopAutoUpdate() {
    if (this.updateSubscription) {
      this.updateSubscription.unsubscribe();
      this.updateSubscription = undefined;
    }
  }

  updateTreeValues(nodes: TreeNode[], newData: any, oldData: any) {
    if (!nodes || !Array.isArray(nodes)) return;
    
    nodes.forEach(node => {
      if (!node || !node.key) return;
      
      // Skip updating if this node is currently being edited
      if (this.editingPaths.has(node.key)) {
        // Still update children if they exist
        if (node.children && Array.isArray(node.children)) {
          this.updateTreeValues(node.children, newData, oldData);
        }
        return;
      }
      
      const newValue = this.getValueByPath(newData, node.key);
      const oldValue = this.getValueByPath(oldData, node.key);
      
      try {
        if (JSON.stringify(newValue) !== JSON.stringify(oldValue)) {
          node.changed = true;
          node.previousValue = oldValue;
          node.value = newValue;
          
          // Only update editValue if not currently being edited
          if (node.editable && !this.editingPaths.has(node.key)) {
            node.editValue = newValue;
          }
          
          // Reset changed flag after animation
          setTimeout(() => {
            node.changed = false;
          }, 500);
        } else {
          node.value = newValue;
          // Only update editValue if not currently being edited
          if (node.editable && !this.editingPaths.has(node.key)) {
            node.editValue = newValue;
          }
        }
      } catch (e) {
        // Si hay error en JSON.stringify (ej: objetos circulares), solo actualizar
        node.value = newValue;
        if (node.editable && !this.editingPaths.has(node.key)) {
          node.editValue = newValue;
        }
      }

      if (node.children && Array.isArray(node.children)) {
        this.updateTreeValues(node.children, newData, oldData);
      }
    });
  }

  buildTree(obj: any, path: string): TreeNode[] {
    const nodes: TreeNode[] = [];

    if (obj === null || obj === undefined) {
      return [{ key: path || 'null', value: null, type: 'null', editable: false }];
    }

    if (Array.isArray(obj)) {
      obj.forEach((item, index) => {
        const newPath = path ? `${path}[${index}]` : `[${index}]`;
        if (typeof item === 'object' && item !== null) {
          nodes.push({
            key: newPath,
            value: item,
            type: 'array',
            children: this.buildTree(item, newPath),
            expanded: false,  // Collapsed by default
            editable: false
          });
        } else {
          nodes.push({
            key: newPath,
            value: item,
            type: typeof item,
            editable: this.isEditableType(item),
            editValue: item
          });
        }
      });
    } else if (typeof obj === 'object') {
      Object.keys(obj).forEach(key => {
        const newPath = path ? `${path}.${key}` : key;
        const value = obj[key];
        
        if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
          nodes.push({
            key: newPath,
            value: value,
            type: 'object',
            children: this.buildTree(value, newPath),
            expanded: false,  // Collapsed by default
            editable: false
          });
        } else if (Array.isArray(value)) {
          nodes.push({
            key: newPath,
            value: value,
            type: 'array',
            children: this.buildTree(value, newPath),
            expanded: false,  // Collapsed by default
            editable: false
          });
        } else {
          nodes.push({
            key: newPath,
            value: value,
            type: typeof value,
            editable: this.isEditableType(value),
            editValue: value
          });
        }
      });
    } else {
      nodes.push({
        key: path || 'value',
        value: obj,
        type: typeof obj,
        editable: this.isEditableType(obj),
        editValue: obj
      });
    }

    return nodes;
  }

  isEditableType(value: any): boolean {
    return typeof value === 'number' || typeof value === 'string' || typeof value === 'boolean';
  }

  getValueByPath(obj: any, path: string): any {
    if (!path) return obj;
    
    // Manejar rutas con arrays: "prop[0].subprop" -> ["prop", "0", "subprop"]
    const parts = path.split(/[\.\[\]]/).filter(p => p !== '');
    let current = obj;
    
    for (const part of parts) {
      if (current === null || current === undefined) {
        return undefined;
      }
      
      // Si es un número, tratar como índice de array
      const numIndex = parseInt(part, 10);
      if (!isNaN(numIndex) && Array.isArray(current)) {
        current = current[numIndex];
      } else {
        current = current[part];
      }
    }
    
    return current;
  }

  onEditingStart(path: string) {
    this.editingPaths.add(path);
  }

  onEditingEnd(path: string) {
    this.editingPaths.delete(path);
  }

  writeValue(data: { path: string; value: any }) {
    if (!this.tagName || !data.path) {
      return;
    }

    const request = {
      tagName: this.tagName,
      path: data.path,
      value: data.value,
      correlationId: `write-${Date.now()}`
    };

    this.mqttService.sendTagWriteRequest(request).subscribe({
      next: () => {
        console.log('✅ Write request sent successfully');
        // La respuesta llegará por SignalR
      },
      error: (err) => {
        this.error = err.error?.error || 'Error sending write request';
        console.error('Error sending write request:', err);
      }
    });
  }

  deepClone(obj: any): any {
    return JSON.parse(JSON.stringify(obj));
  }
}

