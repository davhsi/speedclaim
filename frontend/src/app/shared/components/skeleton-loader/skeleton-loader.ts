import { Component, input } from '@angular/core';

@Component({
  selector: 'app-skeleton',
  standalone: true,
  templateUrl: './skeleton-loader.html',
})
export class SkeletonLoaderComponent {
  variant = input<'card' | 'list-card' | 'table-row' | 'text'>('text');
}
