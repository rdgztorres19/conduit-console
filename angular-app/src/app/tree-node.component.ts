import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface TreeNode {
  key: string;
  value: any;
  type: string;
  children?: TreeNode[];
  expanded?: boolean;
  previousValue?: any;
  changed?: boolean;
  editable?: boolean;
  editValue?: any;
}

@Component({
  selector: 'app-tree-node',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="tree-node-wrapper">
      <div class="tree-node-card" [class.has-children]="node.children" [class.changed]="node.changed">
        <div class="node-content">
          <div class="node-header" (click)="node.children && toggle()">
            <div class="node-icon-wrapper">
              <span *ngIf="node.children" class="expand-icon">
                {{ node.expanded ? '▼' : '▶' }}
              </span>
              <span *ngIf="!node.children" class="leaf-icon">●</span>
            </div>
            <div class="node-label">
              <span class="node-key">{{ getDisplayKey(node.key) }}</span>
              <span class="node-type-badge" [attr.data-type]="node.type">{{ node.type }}</span>
            </div>
          </div>
          
          <div class="node-body">
            <div *ngIf="!node.editable && !node.children" class="value-display" [class.changed]="node.changed">
              <span class="value-text">{{ formatValue(node.value) }}</span>
            </div>
            
            <div *ngIf="node.editable" class="value-edit">
              <input
                type="text"
                [(ngModel)]="node.editValue"
                (blur)="onValueChange()"
                (keyup.enter)="onValueChange()"
                class="value-input"
                placeholder="Enter value"
              />
              <button
                class="btn-write"
                (click)="onWriteClick()"
                [disabled]="writing"
                title="Write to PLC"
              >
                {{ writing ? '⏳' : '✏️' }}
              </button>
            </div>
            
            <div *ngIf="node.children" class="children-count">
              {{ node.children.length }} {{ node.children.length === 1 ? 'item' : 'items' }}
            </div>
          </div>
        </div>
      </div>
      
      <div *ngIf="node.children && node.expanded" class="children-container">
        <div class="connector-line"></div>
        <div class="children-list">
          <app-tree-node
            *ngFor="let child of node.children"
            [node]="child"
            (writeValue)="onWriteValue($event)"
          ></app-tree-node>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .tree-node-wrapper {
      position: relative;
      margin-bottom: 8px;
    }

    .tree-node-card {
      background: #ffffff;
      border: 2px solid #e1e8ed;
      border-radius: 12px;
      padding: 12px 16px;
      transition: all 0.3s ease;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
      position: relative;
    }

    .tree-node-card:hover {
      border-color: #667eea;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.15);
      transform: translateY(-1px);
    }

    .tree-node-card.has-children {
      border-left: 4px solid #667eea;
    }

    .tree-node-card.changed {
      border-color: #ffc107;
      background: #fffbf0;
      animation: highlight 0.6s ease;
    }

    @keyframes highlight {
      0% { background: #fff3cd; }
      100% { background: #fffbf0; }
    }

    .node-content {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .node-header {
      display: flex;
      align-items: center;
      gap: 12px;
      cursor: pointer;
    }

    .node-icon-wrapper {
      width: 24px;
      height: 24px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #f0f4f8;
      border-radius: 6px;
      flex-shrink: 0;
    }

    .expand-icon {
      color: #667eea;
      font-size: 12px;
      font-weight: bold;
    }

    .leaf-icon {
      color: #48bb78;
      font-size: 8px;
    }

    .node-label {
      display: flex;
      align-items: center;
      gap: 10px;
      flex: 1;
    }

    .node-key {
      font-weight: 600;
      color: #2d3748;
      font-size: 15px;
    }

    .node-type-badge {
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .node-type-badge[data-type="object"] {
      background: #e6f3ff;
      color: #0066cc;
    }

    .node-type-badge[data-type="array"] {
      background: #fff4e6;
      color: #cc6600;
    }

    .node-type-badge[data-type="string"] {
      background: #e6ffe6;
      color: #006600;
    }

    .node-type-badge[data-type="number"] {
      background: #ffe6f0;
      color: #cc0066;
    }

    .node-type-badge[data-type="boolean"] {
      background: #f0e6ff;
      color: #6600cc;
    }

    .node-body {
      margin-left: 36px;
    }

    .value-display {
      padding: 8px 12px;
      background: #f7fafc;
      border-radius: 6px;
      border: 1px solid #e2e8f0;
    }

    .value-display.changed {
      background: #fff3cd;
      border-color: #ffc107;
    }

    .value-text {
      font-family: 'Monaco', 'Menlo', 'Courier New', monospace;
      font-size: 13px;
      color: #2d3748;
      word-break: break-word;
    }

    .value-edit {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .value-input {
      flex: 1;
      padding: 8px 12px;
      border: 2px solid #cbd5e0;
      border-radius: 6px;
      font-size: 13px;
      font-family: 'Monaco', 'Menlo', 'Courier New', monospace;
      transition: border-color 0.2s;
    }

    .value-input:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    .btn-write {
      padding: 8px 12px;
      background: #48bb78;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      cursor: pointer;
      transition: all 0.2s;
      display: flex;
      align-items: center;
      justify-content: center;
      min-width: 40px;
    }

    .btn-write:hover:not(:disabled) {
      background: #38a169;
      transform: scale(1.05);
    }

    .btn-write:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .children-count {
      font-size: 12px;
      color: #718096;
      font-style: italic;
      padding: 4px 0;
    }

    .children-container {
      position: relative;
      margin-top: 8px;
      margin-left: 20px;
      padding-left: 20px;
      border-left: 2px dashed #cbd5e0;
    }

    .connector-line {
      position: absolute;
      left: -2px;
      top: 0;
      width: 2px;
      height: 12px;
      background: #cbd5e0;
    }

    .children-list {
      padding-top: 8px;
    }
  `]
})
export class TreeNodeComponent {
  @Input() node!: TreeNode;
  @Output() writeValue = new EventEmitter<{ path: string; value: any }>();
  writing: boolean = false;

  toggle() {
    if (this.node?.children) {
      this.node.expanded = !this.node.expanded;
    }
  }

  getDisplayKey(key: string): string {
    if (!key) return '';
    const parts = key.split(/[\.\[\]]/).filter(p => p);
    return parts[parts.length - 1] || key;
  }

  formatValue(value: any): string {
    if (value === null || value === undefined) {
      return 'null';
    }
    if (typeof value === 'object') {
      return Array.isArray(value) ? `[Array(${value.length})]` : '{Object}';
    }
    return String(value);
  }

  onValueChange() {
    // Convertir el valor al tipo correcto
    const originalType = typeof this.node.value;
    let convertedValue: any = this.node.editValue;
    
    if (originalType === 'number') {
      const parsed = parseFloat(String(this.node.editValue));
      convertedValue = isNaN(parsed) ? this.node.value : parsed;
    } else if (originalType === 'boolean') {
      const strValue = String(this.node.editValue).toLowerCase();
      convertedValue = strValue === 'true' || strValue === '1';
    } else if (originalType === 'string') {
      convertedValue = String(this.node.editValue);
    }
    
    this.node.editValue = convertedValue;
  }

  onWriteClick() {
    if (this.node?.editValue === undefined || this.node?.editValue === null) {
      return;
    }

    this.writing = true;
    this.onValueChange();
    
    this.writeValue.emit({
      path: this.node.key || '',
      value: this.node.editValue
    });

    setTimeout(() => {
      this.writing = false;
    }, 1000);
  }

  onWriteValue(data: { path: string; value: any }) {
    this.writeValue.emit(data);
  }
}
