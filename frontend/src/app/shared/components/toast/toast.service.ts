import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  toasts = signal<Toast[]>([]);

  success(message: string): void { this.add(message, 'success'); }
  error(message: string): void { this.add(message, 'error'); }
  warning(message: string): void { this.add(message, 'warning'); }
  info(message: string): void { this.add(message, 'info'); }

  remove(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }

  private add(message: string, type: Toast['type']): void {
    const id = this.nextId++;
    this.toasts.update(list => [...list, { id, message, type }]);
    setTimeout(() => this.remove(id), 5000);
  }
}
