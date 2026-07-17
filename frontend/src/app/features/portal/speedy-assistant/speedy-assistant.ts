import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SpeedyAssistantService } from '../services/speedy-assistant.service';

interface SpeedyMessage {
  role: 'user' | 'assistant';
  content: string;
}

@Component({
  selector: 'app-speedy-assistant',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './speedy-assistant.html',
})
export class SpeedyAssistantComponent {
  private readonly speedy = inject(SpeedyAssistantService);

  open = signal(false);
  sending = signal(false);
  question = signal('');
  error = signal<string | null>(null);
  messages = signal<SpeedyMessage[]>([]);

  readonly suggestions = [
    'What policies do I have?',
    'When is my next premium due?',
    'What is the status of my claims?',
  ];

  toggle(): void {
    this.open.update(value => !value);
    this.error.set(null);
  }

  close(): void {
    this.open.set(false);
    this.error.set(null);
  }

  ask(value?: string): void {
    const question = (value ?? this.question()).trim();
    if (!question || this.sending()) return;
    this.question.set('');
    this.error.set(null);
    this.messages.update(messages => [...messages, { role: 'user', content: question }]);
    this.sending.set(true);
    this.speedy.ask(question).subscribe({
      next: response => {
        this.messages.update(messages => [...messages, { role: 'assistant', content: response.answer }]);
        this.sending.set(false);
      },
      error: error => {
        this.sending.set(false);
        this.error.set(error?.status === 429
          ? 'Speedy is taking a short breather. Please try again in a minute.'
          : error?.error?.message ?? 'Speedy is temporarily unavailable. Please try again.');
      },
    });
  }

  /**
   * Speedy responses use a small, presentation-only Markdown subset. Content
   * is escaped before formatting so model output can never inject HTML.
   */
  renderMarkdown(content: string): string {
    const lines = content.replace(/\r\n?/g, '\n').split('\n');
    const output: string[] = [];
    let openList: 'ol' | 'ul' | null = null;

    const closeList = (): void => {
      if (openList) output.push(`</${openList}>`);
      openList = null;
    };

    for (const line of lines) {
      const numbered = /^\s*\d+[.)]\s+(.+)$/.exec(line);
      const bullet = /^\s*[-*+]\s+(.+)$/.exec(line);
      const listType = numbered ? 'ol' : bullet ? 'ul' : null;

      if (listType) {
        if (openList && openList !== listType) closeList();
        if (!openList) {
          output.push(`<${listType}>`);
          openList = listType;
        }
        output.push(`<li>${this.renderInlineMarkdown((numbered ?? bullet)![1])}</li>`);
        continue;
      }

      closeList();
      if (!line.trim()) continue;
      output.push(`<p>${this.renderInlineMarkdown(line)}</p>`);
    }

    closeList();
    return output.join('');
  }

  private renderInlineMarkdown(value: string): string {
    return this.escapeHtml(value)
      .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
      .replace(/`([^`]+)`/g, '<code>$1</code>');
  }

  private escapeHtml(value: string): string {
    return value.replace(/[&<>"']/g, character => ({
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#39;',
    })[character]!);
  }
}
