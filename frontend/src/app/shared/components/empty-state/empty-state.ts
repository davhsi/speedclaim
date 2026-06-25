import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SafeHtmlPipe } from '../../pipes/safe-html.pipe';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [RouterLink, SafeHtmlPipe],
  templateUrl: './empty-state.html',
})
export class EmptyStateComponent {
  icon = input<string>('&#128196;');
  title = input<string>('Nothing here yet');
  message = input<string>('');
  actionLabel = input<string>('');
  actionRoute = input<string>('');
}
