import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { SpeedyAssistantComponent } from './speedy-assistant';
import { SpeedyAssistantService } from '../services/speedy-assistant.service';

describe('SpeedyAssistantComponent', () => {
  let speedy: { ask: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    speedy = { ask: vi.fn(() => of({ requestId: 'request-1', answer: 'You have one active policy.' })) };
    TestBed.configureTestingModule({
      imports: [SpeedyAssistantComponent],
      providers: [{ provide: SpeedyAssistantService, useValue: speedy }],
    });
  });

  it('keeps the launcher closed until the customer opens it', () => {
    const fixture = TestBed.createComponent(SpeedyAssistantComponent);
    expect(fixture.componentInstance.open()).toBe(false);
    fixture.componentInstance.toggle();
    expect(fixture.componentInstance.open()).toBe(true);
  });

  it('sends a customer question and appends the read-only answer', () => {
    const fixture = TestBed.createComponent(SpeedyAssistantComponent);
    fixture.componentInstance.ask('What policies do I have?');
    expect(speedy.ask).toHaveBeenCalledWith('What policies do I have?');
    expect(fixture.componentInstance.messages()).toEqual([
      { role: 'user', content: 'What policies do I have?' },
      { role: 'assistant', content: 'You have one active policy.' },
    ]);
  });

  it('renders Speedy Markdown while escaping unsafe HTML', () => {
    const fixture = TestBed.createComponent(SpeedyAssistantComponent);

    expect(fixture.componentInstance.renderMarkdown('**Family Shield**\n\n1. Active cover\n- Premium paid\n<script>alert(1)</script>'))
      .toBe('<p><strong>Family Shield</strong></p><ol><li>Active cover</li></ol><ul><li>Premium paid</li></ul><p>&lt;script&gt;alert(1)&lt;/script&gt;</p>');
  });
});
