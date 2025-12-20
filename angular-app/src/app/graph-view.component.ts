import { Component, Input, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TreeNode, TreeNodeComponent } from './tree-node.component';

// vis-network types are included in the package
declare var vis: {
  Network: any;
  DataSet: any;
};

@Component({
  selector: 'app-graph-view',
  standalone: true,
  imports: [CommonModule, FormsModule, TreeNodeComponent],
  template: `
    <div class="graph-container">
      <div class="graph-controls">
        <button class="btn-graph" (click)="toggleView()">
          {{ showTree ? '📊 Ver Grafo' : '🌳 Ver Árbol' }}
        </button>
        <button class="btn-graph" (click)="fitNetwork()" *ngIf="!showTree">
          🔍 Ajustar Vista
        </button>
        <button class="btn-graph" (click)="resetView()" *ngIf="!showTree">
          🔄 Reset
        </button>
      </div>
      
      <!-- Vista de Grafo -->
      <div #networkContainer class="network-container" *ngIf="!showTree"></div>
      
      <!-- Vista de Árbol -->
      <div class="tree-view-container" *ngIf="showTree">
        <app-tree-node
          *ngFor="let node of treeData"
          [node]="node"
          (writeValue)="onWriteValue($event)"
        ></app-tree-node>
      </div>
    </div>
  `,
  styles: [`
    .graph-container {
      width: 100%;
      height: 100%;
      position: relative;
    }

    .graph-controls {
      display: flex;
      gap: 10px;
      margin-bottom: 15px;
      padding: 10px;
      background: #f8f9fa;
      border-radius: 8px;
    }

    .btn-graph {
      padding: 8px 16px;
      background: #667eea;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn-graph:hover {
      background: #5568d3;
      transform: translateY(-1px);
    }

    .network-container {
      width: 100%;
      height: 600px;
      border: 2px solid #e1e8ed;
      border-radius: 12px;
      background: #ffffff;
    }

    .tree-view-container {
      padding: 10px;
    }
  `]
})
export class GraphViewComponent implements OnChanges, AfterViewInit {
  @Input() treeData: TreeNode[] = [];
  @Output() writeValue = new EventEmitter<{ path: string; value: any }>();
  @ViewChild('networkContainer', { static: false }) networkContainer!: ElementRef;
  
  network: any = null;
  showTree: boolean = false;
  selectedNodeId: string | null = null;

  ngAfterViewInit() {
    if (this.treeData.length > 0) {
      this.updateGraph();
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['treeData'] && !changes['treeData'].firstChange && this.network) {
      this.updateGraph();
    }
  }

  toggleView() {
    this.showTree = !this.showTree;
    if (!this.showTree && this.treeData.length > 0) {
      setTimeout(() => this.updateGraph(), 100);
    }
  }

  fitNetwork() {
    if (this.network) {
      this.network.fit({
        animation: {
          duration: 500,
          easingFunction: 'easeInOutQuad'
        }
      });
    }
  }

  resetView() {
    if (this.network) {
      this.network.setOptions({
        physics: {
          enabled: true
        }
      });
      setTimeout(() => {
        this.network.fit();
      }, 100);
    }
  }

  onWriteValue(data: { path: string; value: any }) {
    this.writeValue.emit(data);
  }

  updateGraph() {
    if (!this.networkContainer || this.showTree) return;

    // Verificar que vis esté disponible
    if (typeof vis === 'undefined') {
      console.error('vis-network library not loaded. Please check the script tag in index.html');
      return;
    }

    try {
      const { nodes, edges } = this.buildGraphData(this.treeData);
      
      const data = {
        nodes: new vis.DataSet(nodes),
        edges: new vis.DataSet(edges)
      };

      const options = {
        nodes: {
          font: {
            size: 13,
            face: 'Arial',
            color: '#2d3748'
          },
          borderWidth: 2,
          shadow: {
            enabled: true,
            color: 'rgba(0,0,0,0.2)',
            size: 5,
            x: 2,
            y: 2
          },
          margin: 8,
          widthConstraint: {
            maximum: 200
          },
          heightConstraint: {
            maximum: 100
          }
        },
        edges: {
          arrows: {
            to: {
              enabled: true,
              scaleFactor: 0.7,
              type: 'arrow'
            }
          },
          smooth: {
            type: 'curvedCW',
            roundness: 0.3
          },
          color: {
            color: '#cbd5e0',
            highlight: '#667eea',
            hover: '#667eea'
          },
          width: 2,
          selectionWidth: 3
        },
        physics: {
          enabled: true,
          stabilization: {
            enabled: true,
            iterations: 200,
            fit: true
          },
          barnesHut: {
            gravitationalConstant: -3000,
            centralGravity: 0.2,
            springLength: 150,
            springConstant: 0.05,
            damping: 0.1,
            avoidOverlap: 0.8
          }
        },
        interaction: {
          hover: true,
          tooltipDelay: 100,
          zoomView: true,
          dragView: true,
          selectConnectedEdges: true,
          navigationButtons: true
        },
        layout: {
          improvedLayout: true
        }
      };

      if (this.network) {
        this.network.destroy();
      }

      this.network = new vis.Network(this.networkContainer.nativeElement, data, options);

      // Event listeners
      this.network.on('click', (params: any) => {
        if (params.nodes.length > 0) {
          const nodeId = params.nodes[0];
          const nodeData = nodes.find((n: any) => n.id === nodeId);
          if (nodeData && nodeData.editable) {
            this.selectedNodeId = nodeId;
            const newValue = prompt(`Enter new value for ${nodeData.label.split(':')[0]}:`, nodeData.nodeData.editValue);
            if (newValue !== null) {
              this.onWriteValue({
                path: nodeData.nodeData.key,
                value: newValue
              });
            }
          }
        } else {
          this.selectedNodeId = null;
        }
      });

      this.network.on('stabilizationEnd', () => {
        this.network.fit();
      });

      this.network.on('hoverNode', (params: any) => {
        this.networkContainer.nativeElement.style.cursor = 'pointer';
      });

      this.network.on('blurNode', () => {
        this.networkContainer.nativeElement.style.cursor = 'default';
      });

    } catch (error) {
      console.error('Error creating network:', error);
    }
  }

  buildGraphData(treeData: TreeNode[], parentId: string | null = null, nodeIdCounter: { value: number } = { value: 0 }): { nodes: any[], edges: any[] } {
    const nodes: any[] = [];
    const edges: any[] = [];

    const processNode = (node: TreeNode, parent: string | null): string => {
      const id = `node_${nodeIdCounter.value++}`;
      
      // Determinar color según tipo
      const displayKey = this.getDisplayKey(node.key);
      let color = '#e1e8ed';
      let label = displayKey;
      
      if (node.type === 'object') {
        color = '#4A90E2';
        label = `${displayKey}\n{Object}`;
      } else if (node.type === 'array') {
        color = '#F5A623';
        label = `${displayKey}\n[Array(${node.children?.length || 0})]`;
      } else if (node.type === 'string') {
        color = '#7ED321';
        const val = this.formatValue(node.value);
        label = `${displayKey}\n"${val.length > 15 ? val.substring(0, 15) + '...' : val}"`;
      } else if (node.type === 'number') {
        color = '#BD10E0';
        label = `${displayKey}\n${node.value}`;
      } else if (node.type === 'boolean') {
        color = '#9013FE';
        label = `${displayKey}\n${node.value}`;
      }

      // Si es editable, agregar indicador
      if (node.editable) {
        label += '\n✏️';
      }

      const nodeConfig: any = {
        id: id,
        label: label,
        color: {
          background: color,
          border: node.changed ? '#ffc107' : (node.editable ? '#48bb78' : color),
          highlight: {
            background: node.editable ? '#48bb78' : color,
            border: '#667eea'
          }
        },
        shape: node.children ? 'box' : (node.editable ? 'diamond' : 'ellipse'),
        font: {
          color: '#2d3748',
          size: node.children ? 14 : 12,
          bold: node.editable
        },
        value: node.children ? (node.children.length * 10) : (node.editable ? 8 : 5),
        editable: node.editable,
        nodeData: node,
        borderWidth: node.changed ? 4 : 2,
        shadow: true
      };

      if (node.editable) {
        nodeConfig.title = `Click to edit: ${node.key}\nCurrent value: ${this.formatValue(node.value)}`;
      }

      nodes.push(nodeConfig);

      // Crear edge desde el padre
      if (parent !== null) {
        edges.push({
          from: parent,
          to: id,
          smooth: true
        });
      }

      // Procesar hijos
      if (node.children) {
        node.children.forEach(child => {
          processNode(child, id);
        });
      }

      return id;
    };

    treeData.forEach(node => {
      processNode(node, parentId);
    });

    return { nodes, edges };
  }

  formatValue(value: any): string {
    if (value === null || value === undefined) {
      return 'null';
    }
    if (typeof value === 'object') {
      return Array.isArray(value) ? `[Array(${value.length})]` : '{Object}';
    }
    const str = String(value);
    return str.length > 20 ? str.substring(0, 20) + '...' : str;
  }

  getDisplayKey(key: string): string {
    if (!key) return '';
    const parts = key.split(/[\.\[\]]/).filter(p => p);
    return parts[parts.length - 1] || key;
  }
}
