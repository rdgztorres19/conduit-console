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
          {{ showTree ? '📊 View Graph' : '🌳 View Tree' }}
        </button>
        <button class="btn-graph" (click)="fitNetwork()" *ngIf="!showTree">
          🔍 Fit View
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
      min-height: 600px;
      height: 70vh;
      border: 2px solid #e1e8ed;
      border-radius: 12px;
      background: #ffffff;
      position: relative;
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
  showTree: boolean = false;  // Default to graph view
  selectedNodeId: string | null = null;

  ngAfterViewInit() {
    // Wait for vis-network to load
    this.waitForVisNetwork().then(() => {
      if (this.treeData && this.treeData.length > 0) {
        // Small delay to ensure container is rendered
        setTimeout(() => {
          this.updateGraph();
        }, 100);
      }
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['treeData']) {
      if (changes['treeData'].firstChange) {
        // First change - wait for view init
        return;
      }
      // Data changed - update graph
      this.waitForVisNetwork().then(() => {
        setTimeout(() => this.updateGraph(), 50);
      });
    }
  }

  toggleView() {
    this.showTree = !this.showTree;
    if (!this.showTree && this.treeData.length > 0) {
      this.waitForVisNetwork().then(() => {
        setTimeout(() => this.updateGraph(), 100);
      });
    }
  }

  private waitForVisNetwork(): Promise<void> {
    return new Promise((resolve) => {
      if (typeof vis !== 'undefined' && vis.Network) {
        resolve();
        return;
      }

      // Intentar esperar hasta que vis esté disponible
      let attempts = 0;
      const maxAttempts = 50;
      const checkInterval = setInterval(() => {
        attempts++;
        if (typeof vis !== 'undefined' && vis.Network) {
          clearInterval(checkInterval);
          resolve();
        } else if (attempts >= maxAttempts) {
          clearInterval(checkInterval);
          console.error('vis-network failed to load after 5 seconds');
          resolve(); // Resolver de todos modos para no bloquear
        }
      }, 100);
    });
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
          enabled: false
        }
      });
      setTimeout(() => {
        this.network.fit({
          animation: {
            duration: 500,
            easingFunction: 'easeInOutQuad'
          }
        });
      }, 100);
    }
  }

  onWriteValue(data: { path: string; value: any }) {
    this.writeValue.emit(data);
  }

  updateGraph() {
    if (this.showTree) return;
    
    if (!this.networkContainer) {
      console.warn('Network container not available');
      return;
    }
    
    if (!this.networkContainer.nativeElement) {
      console.warn('Network container element not available');
      return;
    }

    // Check if vis is available
    if (typeof vis === 'undefined' || !vis.Network || !vis.DataSet) {
      console.error('vis-network library not loaded. Please check the script tag in index.html');
      return;
    }

    if (!this.treeData || this.treeData.length === 0) {
      console.warn('No tree data to display');
      return;
    }

    try {
      const { nodes, edges } = this.buildGraphData(this.treeData);
      
      if (nodes.length === 0) {
        console.warn('No nodes to display after building graph data');
        return;
      }
      
      console.log(`Building graph with ${nodes.length} nodes and ${edges.length} edges`);
      
      const data = {
        nodes: new vis.DataSet(nodes),
        edges: new vis.DataSet(edges)
      };

      const options = {
        nodes: {
          font: {
            size: 14,
            face: 'Arial',
            color: '#111827', // Black text
            bold: false
          },
          borderWidth: 3,
          shadow: {
            enabled: true,
            color: 'rgba(0,0,0,0.3)',
            size: 8,
            x: 3,
            y: 3
          },
          margin: 15,
          widthConstraint: {
            minimum: 180,
            maximum: 350  // Narrower but still accommodates full paths
          },
          heightConstraint: {
            minimum: 50,
            maximum: 120  // Reduced height for more compact vertical layout
          },
          shapeProperties: {
            borderRadius: 8
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
            type: 'straight'
          },
          color: {
            color: '#6B7280', // Gray edges
            highlight: '#111827', // Black on highlight
            hover: '#374151' // Dark gray on hover
          },
          width: 3,  // Thicker edges for better visibility
          selectionWidth: 4
        },
        physics: {
          enabled: false  // Disable physics - hierarchical layout handles positioning
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
          hierarchical: {
            enabled: true,
            direction: 'UD',  // Up to Down (Top to Bottom) - more narrow
            sortMethod: 'directed',
            levelSeparation: 120,  // Reduced vertical spacing between levels
            nodeSpacing: 200,      // Horizontal spacing between nodes
            treeSpacing: 150,      // Reduced spacing between trees
            blockShifting: true,
            edgeMinimization: true,
            parentCentralization: true,
            shakeTowards: 'roots'
          }
        }
      };

      if (this.network) {
        this.network.destroy();
        this.network = null;
      }

      // Ensure container is visible and has dimensions
      const container = this.networkContainer.nativeElement;
      if (container.offsetWidth === 0 || container.offsetHeight === 0) {
        console.warn('Container has no dimensions, waiting...');
        setTimeout(() => this.updateGraph(), 100);
        return;
      }

      this.network = new vis.Network(container, data, options);
      
      console.log('Network created successfully');

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

      // Fit view after network is created
      setTimeout(() => {
        if (this.network) {
          this.network.fit({
            animation: {
              duration: 500,
              easingFunction: 'easeInOutQuad'
            }
          });
        }
      }, 300);

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

  buildGraphData(treeData: TreeNode[], parentId: string | null = null, nodeIdCounter: { value: number } = { value: 0 }, level: number = 0): { nodes: any[], edges: any[] } {
    const nodes: any[] = [];
    const edges: any[] = [];
    let isRoot = true;

    const processNode = (node: TreeNode, parent: string | null, currentLevel: number, isRootNode: boolean = false): string => {
      const id = `node_${nodeIdCounter.value++}`;
      
      // Use full path for all nodes (not just display key)
      const fullPath = node.key || '';
      let color = '#E5E7EB'; // Light gray default
      let label = fullPath;
      
      // Mark root node - show full path
      if (isRootNode) {
        label = fullPath;
        color = '#1F2937'; // Dark gray/black
      } else if (node.type === 'object') {
        color = '#4B5563'; // Medium gray
        label = fullPath;
      } else if (node.type === 'array') {
        color = '#6B7280'; // Lighter gray
        label = `${fullPath}[${node.children?.length || 0}]`;
      } else if (node.type === 'string') {
        color = '#9CA3AF'; // Light gray
        const val = this.formatValue(node.value);
        label = `${fullPath}: ${val.length > 25 ? val.substring(0, 25) + '...' : val}`;  // Single line for compactness
      } else if (node.type === 'number') {
        color = '#D1D5DB'; // Very light gray
        label = `${fullPath}: ${node.value}`;  // Single line
      } else if (node.type === 'boolean') {
        color = '#E5E7EB'; // Lightest gray
        label = `${fullPath}: ${node.value}`;  // Single line
      }

      // If editable, add indicator (inline for better readability)
      if (node.editable) {
        label += ' ✏️';
      }

      const nodeConfig: any = {
        id: id,
        label: label,
        color: {
          background: isRootNode ? '#111827' : color, // Black for root, gray for others
          border: isRootNode ? '#374151' : (node.changed ? '#F59E0B' : (node.editable ? '#6B7280' : '#374151')),
          highlight: {
            background: isRootNode ? '#1F2937' : (node.editable ? '#4B5563' : '#9CA3AF'),
            border: '#111827' // Black border on highlight
          }
        },
        shape: isRootNode ? 'box' : (node.children ? 'box' : (node.editable ? 'diamond' : 'ellipse')),
        font: {
          color: isRootNode ? '#FFFFFF' : '#111827', // White text on root, black on others
          size: isRootNode ? 15 : (node.children ? 13 : 12),  // Slightly smaller for compact layout
          bold: isRootNode || node.editable
        },
        value: isRootNode ? 20 : (node.children ? (node.children.length * 10) : (node.editable ? 8 : 5)),
        editable: node.editable,
        nodeData: node,
        borderWidth: isRootNode ? 4 : (node.changed ? 4 : 2),
        shadow: true,
        level: currentLevel  // Set level for hierarchical layout
      };

      if (node.editable) {
        nodeConfig.title = `Click to edit: ${node.key}\nCurrent value: ${this.formatValue(node.value)}`;
      }

      nodes.push(nodeConfig);

      // Create edge from parent
      if (parent !== null) {
        edges.push({
          from: parent,
          to: id
        });
      }

      // Process children
      if (node.children && node.children.length > 0) {
        node.children.forEach(child => {
          processNode(child, id, currentLevel + 1, false);
        });
      }

      return id;
    };

    treeData.forEach(node => {
      processNode(node, parentId, level, isRoot);
      isRoot = false; // Only first node is root
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
