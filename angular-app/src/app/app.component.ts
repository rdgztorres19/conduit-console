import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
import { TreeNodeComponent, TreeNode } from './tree-node.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, TreeNodeComponent],
  styleUrls: [],
  template: `
    <div class="container">
      <div class="header">
        <h1>🔌 PLC Tag Monitor</h1>
      </div>

      <div class="controls">
        <div class="input-group">
          <label for="tagName">Tag Name:</label>
          <input
            id="tagName"
            type="text"
            [(ngModel)]="tagName"
            placeholder="Enter tag name"
            (keyup.enter)="loadStructure()"
          />
        </div>
        <button
          class="btn btn-primary"
          (click)="loadStructure()"
          [disabled]="loading || !tagName.trim()"
        >
          🔍 Buscar Estructura
        </button>
      </div>

      <div *ngIf="error" class="error">
        ❌ {{ error }}
      </div>

      <div *ngIf="status" class="status" [class.connected]="isConnected" [class.disconnected]="!isConnected">
        <span class="status-indicator" [class.active]="isConnected" [class.inactive]="!isConnected"></span>
        {{ status }}
      </div>

      <div *ngIf="loading && !treeData" class="loading">
        ⏳ Cargando estructura...
      </div>

      <div *ngIf="!loading && !treeData && !error" class="empty-state">
        Ingresa un tag name y haz clic en "Buscar Estructura" para comenzar
      </div>

      <div *ngIf="treeData" class="tree-container">
        <app-tree-node
          *ngFor="let node of treeData"
          [node]="node"
          (writeValue)="writeValue($event)"
        ></app-tree-node>
      </div>
    </div>
  `,
  styles: []
})
export class AppComponent implements OnInit, OnDestroy {
  tagName: string = 'ngpSampleCurrent';
  treeData: TreeNode[] = [];
  loading: boolean = false;
  error: string = '';
  status: string = '';
  isConnected: boolean = false;
  private updateSubscription?: Subscription;
  private previousData: any = null;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.checkStatus();
  }

  ngOnDestroy() {
    if (this.updateSubscription) {
      this.updateSubscription.unsubscribe();
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
    if (this.updateSubscription) {
      this.updateSubscription.unsubscribe();
    }

    this.http.get<any>(`/api/plc/tags/${encodeURIComponent(this.tagName)}`).subscribe({
      next: (response) => {
        this.loading = false;
        
        if (response.quality !== 'Good') {
          this.error = `Tag quality: ${response.quality}`;
          return;
        }

        this.treeData = this.buildTree(response.value, '');
        this.previousData = this.deepClone(response.value);
        
        // Iniciar actualizaciones cada segundo
        this.startAutoUpdate();
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.error || err.message || 'Error al cargar el tag';
        console.error('Error loading tag:', err);
      }
    });
  }

  startAutoUpdate() {
    this.updateSubscription = interval(1000).subscribe(() => {
      this.updateValues();
    });
  }

  updateValues() {
    this.http.get<any>(`/api/plc/tags/${encodeURIComponent(this.tagName)}`).subscribe({
      next: (response) => {
        if (response.quality === 'Good' && response.value) {
          this.updateTreeValues(this.treeData, response.value, this.previousData);
          this.previousData = this.deepClone(response.value);
        }
      },
      error: (err) => {
        console.error('Error updating values:', err);
      }
    });
  }

  updateTreeValues(nodes: TreeNode[], newData: any, oldData: any) {
    if (!nodes || !Array.isArray(nodes)) return;
    
    nodes.forEach(node => {
      if (!node || !node.key) return;
      
      const newValue = this.getValueByPath(newData, node.key);
      const oldValue = this.getValueByPath(oldData, node.key);
      
      try {
        if (JSON.stringify(newValue) !== JSON.stringify(oldValue)) {
          node.changed = true;
          node.previousValue = oldValue;
          node.value = newValue;
          
          // Actualizar editValue si es editable
          if (node.editable) {
            node.editValue = newValue;
          }
          
          // Reset changed flag after animation
          setTimeout(() => {
            node.changed = false;
          }, 500);
        } else {
          node.value = newValue;
          // Actualizar editValue si es editable
          if (node.editable) {
            node.editValue = newValue;
          }
        }
      } catch (e) {
        // Si hay error en JSON.stringify (ej: objetos circulares), solo actualizar
        node.value = newValue;
        if (node.editable) {
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
            expanded: true,
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
            expanded: true,
            editable: false
          });
        } else if (Array.isArray(value)) {
          nodes.push({
            key: newPath,
            value: value,
            type: 'array',
            children: this.buildTree(value, newPath),
            expanded: true,
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

  writeValue(data: { path: string; value: any }) {
    // Enviar solo el path y el valor cuando es un primitivo
    const request = {
      path: data.path,
      value: data.value
    };

    this.http.post(`/api/plc/tags/${encodeURIComponent(this.tagName)}/write-path`, request).subscribe({
      next: () => {
        console.log('Value written successfully');
        // Refrescar la estructura general después de escribir
        this.loadStructure();
      },
      error: (err) => {
        this.error = err.error?.error || 'Error al escribir el valor';
        console.error('Error writing value:', err);
      }
    });
  }

  deepClone(obj: any): any {
    return JSON.parse(JSON.stringify(obj));
  }
}

