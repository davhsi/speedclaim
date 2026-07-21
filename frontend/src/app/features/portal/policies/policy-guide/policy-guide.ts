import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PolicyAssistantAnswer, PolicyAssistantAvailability, PolicyAssistantCitation, PolicyDto } from '../../../../core/models/api.models';
import { PolicyService } from '../services/policy.service';

interface GuideExchange {
  question: string;
  answer: string;
  evidenceStatus: PolicyAssistantAnswer['evidenceStatus'];
  citations: PolicyAssistantCitation[];
}

@Component({
  selector: 'app-policy-guide',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './policy-guide.html',
})
export class PolicyGuideComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly policyService = inject(PolicyService);
  readonly router = inject(Router);
  readonly policyId = this.route.snapshot.paramMap.get('id') ?? '';

  readonly policy = signal<PolicyDto | null>(null);
  readonly availability = signal<PolicyAssistantAvailability | null>(null);
  readonly question = signal('');
  readonly exchanges = signal<GuideExchange[]>([]);
  readonly loading = signal(true);
  readonly sending = signal(false);
  readonly error = signal<string | null>(null);
  private readonly conversationId = signal<string | null>(null);
  readonly isReady = computed(() => this.availability()?.available === true);

  constructor() {
    if (!this.policyId) {
      this.loading.set(false);
      this.error.set('This policy could not be identified.');
      return;
    }
    this.policyService.getById(this.policyId).subscribe({
      next: policy => this.policy.set(policy),
      error: () => this.error.set('This policy could not be loaded.'),
    });
    this.policyService.getGuideAvailability(this.policyId).subscribe({
      next: availability => {
        this.availability.set(availability);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('The Policy Guide is unavailable right now. Please try again later.');
        this.loading.set(false);
      },
    });
  }

  ask(): void {
    const question = this.question().trim();
    if (!question || this.sending() || !this.isReady()) return;
    this.error.set(null);
    this.sending.set(true);
    const conversationId = this.conversationId();
    if (conversationId) {
      this.sendQuestion(conversationId, question);
      return;
    }
    this.policyService.createGuideConversation(this.policyId).subscribe({
      next: conversation => {
        this.conversationId.set(conversation.id);
        this.sendQuestion(conversation.id, question);
      },
      error: () => this.handleSendFailure(),
    });
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
      event.preventDefault();
      this.ask();
    }
  }

  private handleSendFailure(): void {
    this.sending.set(false);
    this.error.set('The Policy Guide could not answer right now. Please try again.');
  }

  private sendQuestion(conversationId: string, question: string): void {
    this.policyService.askGuide(this.policyId, conversationId, question).subscribe({
      next: answer => {
        this.exchanges.update(items => [...items, { question, answer: answer.answer, evidenceStatus: answer.evidenceStatus, citations: answer.citations }]);
        this.question.set('');
        this.sending.set(false);
      },
      error: () => this.handleSendFailure(),
    });
  }
}
