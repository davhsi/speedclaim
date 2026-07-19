import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { concatMap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { SpeedyWorkspaceAction, SpeedyWorkspaceConversation } from '../../../core/models/api.models';
import { ProfileService } from '../profile/services/profile.service';
import { SpeedyAssistantService } from '../services/speedy-assistant.service';

interface WorkspaceMessage {
  role: 'user' | 'assistant';
  content: string;
  actions?: SpeedyWorkspaceAction[];
}

const AADHAAR_PATTERN = /^\d{12}$/;
const PAN_PATTERN = /^[A-Z]{5}\d{4}[A-Z]$/;

interface BrowserSpeechRecognition {
  lang: string;
  interimResults: boolean;
  maxAlternatives: number;
  onstart: (() => void) | null;
  onend: (() => void) | null;
  onerror: (() => void) | null;
  onresult: ((event: { results: ArrayLike<{ 0: { transcript: string } }> }) => void) | null;
  start(): void;
}

@Component({
  selector: 'app-speedy-workspace',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './speedy-workspace.html',
})
export class SpeedyWorkspaceComponent {
  private readonly speedy = inject(SpeedyAssistantService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly profile = inject(ProfileService);

  readonly signedIn = computed(() => this.auth.currentUser()?.role === 'Customer');
  readonly question = signal('');
  readonly messages = signal<WorkspaceMessage[]>([]);
  readonly conversationId = signal<string | null>(null);
  readonly conversations = signal<SpeedyWorkspaceConversation[]>([]);
  readonly sectionNavigatorOpen = signal(false);
  readonly historyLoaded = signal(false);
  readonly historyError = signal(false);
  readonly sending = signal(false);
  readonly error = signal<string | null>(null);
  readonly voiceAvailable = signal(typeof window !== 'undefined' && ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window));
  readonly listening = signal(false);
  readonly showKyc = signal(false);
  readonly kycSubmitting = signal(false);
  readonly kycError = signal<string | null>(null);
  readonly aadhaarNumber = signal('');
  readonly panNumber = signal('');
  readonly aadhaarFile = signal<File | null>(null);
  readonly panFile = signal<File | null>(null);
  readonly kycReady = computed(() => AADHAAR_PATTERN.test(this.aadhaarNumber().trim())
    && PAN_PATTERN.test(this.panNumber().trim().toUpperCase()) && !!this.aadhaarFile() && !!this.panFile());

  readonly suggestions = computed(() => this.signedIn()
    ? ['Which policy may help with a hospital admission?', 'Help me complete KYC', 'When is my next premium due?', 'I need to raise a grievance']
    : ['Compare family health cover', 'Which products are available?', 'How do I start a quote?']);
  readonly recentConversations = computed(() => this.conversations().filter(conversation => this.ageInDays(conversation.updatedAt) <= 7));
  readonly previousConversations = computed(() => this.conversations().filter(conversation => {
    const days = this.ageInDays(conversation.updatedAt);
    return days > 7 && days <= 30;
  }));

  constructor() {
    effect(() => {
      if (this.signedIn() && !this.historyLoaded()) this.loadConversations();
    });
  }

  ask(value?: string): void {
    const question = (value ?? this.question()).trim();
    if (!question || this.sending()) return;
    this.question.set('');
    this.error.set(null);
    this.messages.update(messages => [...messages, { role: 'user', content: question }]);
    this.sending.set(true);
    this.speedy.askWorkspace(question, this.conversationId()).subscribe({
      next: response => {
        if (response.conversationId) this.conversationId.set(response.conversationId);
        this.messages.update(messages => [...messages, { role: 'assistant', content: response.answer, actions: response.actions }]);
        this.sending.set(false);
        if (this.signedIn()) this.refreshConversations();
      },
      error: failure => {
        this.sending.set(false);
        this.error.set(failure?.status === 401 ? 'Sign in to use account-specific help.' : 'Speedy is temporarily unavailable. Please try again.');
      },
    });
  }

  startNewChat(): void {
    if (this.sending()) return;
    this.sectionNavigatorOpen.set(false);
    this.conversationId.set(null);
    this.messages.set([]);
    this.question.set('');
    this.error.set(null);
  }

  openConversation(conversationId: string): void {
    if (this.sending() || this.conversationId() === conversationId) return;
    this.error.set(null);
    this.speedy.getWorkspaceConversation(conversationId).subscribe({
      next: conversation => {
        this.sectionNavigatorOpen.set(false);
        this.conversationId.set(conversation.id);
        this.messages.set((conversation.messages ?? []).map(message => ({
          role: message.role.toLowerCase() as WorkspaceMessage['role'],
          content: message.content,
          actions: message.actions,
        })));
      },
      error: () => this.error.set('That conversation could not be opened. Please try again.'),
    });
  }

  toggleSectionNavigator(): void {
    this.sectionNavigatorOpen.update(open => !open);
  }

  jumpToMessage(index: number): void {
    if (typeof document !== 'undefined') document.getElementById(`speedy-message-${index}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    this.sectionNavigatorOpen.set(false);
  }

  runAction(action: SpeedyWorkspaceAction): void {
    if (action.kind === 'guided_kyc') {
      this.showKyc.set(true);
      this.announce('I’ve opened the secure KYC checklist. Attach both labelled documents before continuing.');
      return;
    }
    if (action.kind === 'navigate' && action.route) this.router.navigateByUrl(action.route);
  }

  chooseFile(kind: 'aadhaar' | 'pan', event: Event): void {
    const file = (event.target as HTMLInputElement).files?.item(0) ?? null;
    if (kind === 'aadhaar') this.aadhaarFile.set(file);
    else this.panFile.set(file);
  }

  submitKyc(): void {
    if (!this.kycReady() || this.kycSubmitting()) return;
    this.kycSubmitting.set(true);
    this.kycError.set(null);
    this.profile.uploadAadhaar(this.aadhaarFile()!, this.aadhaarNumber().trim()).pipe(
      concatMap(() => this.profile.uploadPan(this.panFile()!, this.panNumber().trim().toUpperCase())),
    ).subscribe({
      next: () => {
        this.kycSubmitting.set(false);
        this.showKyc.set(false);
        this.messages.update(messages => [...messages, { role: 'assistant', content: 'Your Aadhaar and PAN were sent for review. KYC approval remains a human review step.' }]);
        this.announce('Your KYC documents were sent for review.');
      },
      error: () => {
        this.kycSubmitting.set(false);
        this.kycError.set('We could not upload both documents. Any completed upload remains visible in KYC, where you can safely finish the missing document.');
      },
    });
  }

  toggleVoice(): void {
    if (this.listening()) return;
    const BrowserWindow = window as Window & { SpeechRecognition?: new () => BrowserSpeechRecognition; webkitSpeechRecognition?: new () => BrowserSpeechRecognition };
    const Recognition = BrowserWindow.SpeechRecognition ?? BrowserWindow.webkitSpeechRecognition;
    if (!Recognition) {
      this.error.set('Voice input is not available in this browser. You can continue by typing.');
      return;
    }
    const recognition = new Recognition();
    recognition.lang = 'en-IN';
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;
    recognition.onstart = () => this.listening.set(true);
    recognition.onend = () => this.listening.set(false);
    recognition.onerror = () => { this.listening.set(false); this.error.set('Voice input could not be captured. Please try again or type your question.'); };
    recognition.onresult = event => this.question.set(event.results[0][0].transcript);
    recognition.start();
  }

  /**
   * Speedy returns a deliberately small Markdown subset. Escape first so model
   * output cannot introduce executable HTML, then add only presentation tags.
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
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;',
    })[character]!);
  }

  private announce(text: string): void {
    if ('speechSynthesis' in window) window.speechSynthesis.speak(new SpeechSynthesisUtterance(text));
  }

  private loadConversations(): void {
    this.historyLoaded.set(true);
    this.refreshConversations();
  }

  private refreshConversations(): void {
    this.speedy.listWorkspaceConversations().subscribe({
      next: conversations => {
        this.conversations.set(conversations);
        this.historyError.set(false);
      },
      error: () => this.historyError.set(true),
    });
  }

  private ageInDays(value: string): number {
    return Math.max(0, (Date.now() - new Date(value).getTime()) / 86_400_000);
  }
}
