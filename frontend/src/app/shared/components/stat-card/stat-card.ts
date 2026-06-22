import { Component, input } from '@angular/core';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  templateUrl: './stat-card.html',
})
export class StatCardComponent {
  title = input.required<string>();
  value = input.required<string>();
  icon = input<string>('');
  subtitle = input<string>('');
  iconBgClass = input<string>('bg-primary-light text-primary');
}
