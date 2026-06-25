import { Component, input } from '@angular/core';
import { SafeHtmlPipe } from '../../pipes/safe-html.pipe';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [SafeHtmlPipe],
  templateUrl: './stat-card.html',
})
export class StatCardComponent {
  title = input.required<string>();
  value = input.required<string>();
  icon = input<string>('');
  subtitle = input<string>('');
  iconBgClass = input<string>('bg-primary-light text-primary');
}
