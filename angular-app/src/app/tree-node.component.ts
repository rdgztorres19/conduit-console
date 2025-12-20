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
    <div class="tree-node">
      <div class="tree-node-header">
        <span *ngIf="node.children" class="toggle-icon" (click)="toggle()">
          {{ node.expanded ? '▼' : '▶' }}
        </span>
        <span *ngIf="!node.children" class="toggle-icon" style="opacity: 0;">•</span>
        <span class="tree-node-key">{{ getDisplayKey(node.key) }}</span>
        <div class="tree-node-value">
          <span *ngIf="!node.editable" class="value-badge" [class.changed]="node.changed">
            {{ formatValue(node.value) }}
          </span>
          <input
            *ngIf="node.editable"
            type="text"
            [(ngModel)]="node.editValue"
            (blur)="onValueChange()"
            (keyup.enter)="onValueChange()"
            class="value-input"
          />
          <span class="tree-node-type">({{ node.type }})</span>
          <button
            *ngIf="node.editable"
            class="btn-write"
            (click)="onWriteClick()"
            [disabled]="writing"
          >
            {{ writing ? '⏳' : '✏️' }}
          </button>
        </div>
      </div>
      <div *ngIf="node.children && node.expanded">
        <app-tree-node
          *ngFor="let child of node.children"
          [node]="child"
          (writeValue)="onWriteValue($event)"
        ></app-tree-node>
      </div>
    </div>
  `,
  styles: [`
    .value-input {
      padding: 4px 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
      width: 150px;
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
