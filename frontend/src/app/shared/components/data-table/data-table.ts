import { Component, input, output, signal } from '@angular/core';
import { EmptyStateComponent } from '../empty-state/empty-state';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [EmptyStateComponent],
  templateUrl: './data-table.html',
})
export class DataTableComponent {
  columns = input<TableColumn[]>([]);
  data = input<any[]>([]);
  loading = input(false);
  emptyTitle = input('No data');
  emptyMessage = input('');

  rowClick = output<any>();

  sortKey = signal('');
  sortDir = signal<'asc' | 'desc'>('asc');

  skeletonRows = new Array(5);

  toggleSort(key: string): void {
    if (this.sortKey() === key) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortKey.set(key);
      this.sortDir.set('asc');
    }
  }

  sortedData(): any[] {
    const key = this.sortKey();
    if (!key) return this.data();
    const dir = this.sortDir() === 'asc' ? 1 : -1;
    return [...this.data()].sort((a, b) => {
      const va = a[key], vb = b[key];
      if (va == null) return 1;
      if (vb == null) return -1;
      return this.compareValues(va, vb, dir);
    });
  }

  private compareValues(va: any, vb: any, dir: number): number {
    if (va > vb) return dir;
    if (va < vb) return -dir;
    return 0;
  }
}
