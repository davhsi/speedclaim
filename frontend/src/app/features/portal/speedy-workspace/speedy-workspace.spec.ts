import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../../../core/services/auth.service';
import { ProfileService } from '../profile/services/profile.service';
import { SpeedyAssistantService } from '../services/speedy-assistant.service';
import { SpeedyWorkspaceComponent } from './speedy-workspace';

describe('SpeedyWorkspaceComponent', () => {
  const speedy = {
    askWorkspace: vi.fn(() => of({
      requestId: 'request-1', answer: 'Open your KYC checklist.', intent: 'kyc', risk: 'regulated',
      actions: [{ kind: 'guided_kyc', label: 'Complete KYC', route: null, detail: 'Attach both documents.', requiresConfirmation: true }],
    })),
    listWorkspaceConversations: vi.fn(() => of([])),
    getWorkspaceConversation: vi.fn(),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SpeedyWorkspaceComponent],
      providers: [
        provideRouter([]),
        { provide: SpeedyAssistantService, useValue: speedy },
        { provide: ProfileService, useValue: { getKyc: vi.fn(), uploadAadhaar: vi.fn(), uploadPan: vi.fn() } },
        { provide: AuthService, useValue: { currentUser: signal(null) } },
      ],
    });
  });

  it('renders the typed action returned by the supervised workspace endpoint', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.ask('Help me complete KYC');

    expect(speedy.askWorkspace).toHaveBeenCalledWith('Help me complete KYC', null);
    expect(fixture.componentInstance.messages()[1].actions?.[0].kind).toBe('guided_kyc');
  });

  it('prepares a guided prompt in the composer without sending it immediately', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    speedy.askWorkspace.mockClear();

    fixture.componentInstance.prepareQuestion('Help me complete my KYC.');

    expect(fixture.componentInstance.question()).toBe('Help me complete my KYC.');
    expect(speedy.askWorkspace).not.toHaveBeenCalled();
  });

  it('marks KYC as submitted while keeping the next-step prompt available', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.kycRecord.set({
      id: 'kyc-1', userId: 'user-1', kycStatus: 'Pending', aadhaarUploaded: true, panUploaded: true,
      createdAt: '',
    });

    const card = fixture.componentInstance.journeyCards()[0];
    expect(card).toMatchObject({ title: 'KYC submitted', complete: true, prompt: 'My KYC is submitted. What should I do next?' });
  });

  it('opens the labelled KYC composer rather than navigating to an untrusted route', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.runAction({ kind: 'guided_kyc', label: 'Complete KYC', route: null, detail: 'Attach both documents.', requiresConfirmation: true });

    expect(fixture.componentInstance.showKyc()).toBe(true);
  });

  it('renders Speedy Markdown while escaping unsafe HTML', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);

    expect(fixture.componentInstance.renderMarkdown('**Health cover**\n\n1. Compare products\n- Check waiting period\n<script>alert(1)</script>'))
      .toBe('<p><strong>Health cover</strong></p><ol><li>Compare products</li></ol><ul><li>Check waiting period</li></ul><p>&lt;script&gt;alert(1)&lt;/script&gt;</p>');
  });

  it('renders Markdown tables as accessible table markup', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);

    expect(fixture.componentInstance.renderMarkdown('| Due date | Amount |\n| --- | ---: |\n| 20 Aug 2026 | ₹6,800 |'))
      .toContain('<table class="min-w-full border-collapse text-left text-sm">');
    expect(fixture.componentInstance.renderMarkdown('| Due date | Amount |\n| --- | ---: |\n| 20 Aug 2026 | ₹6,800 |'))
      .toContain('<th class="whitespace-nowrap border-b border-[#DCE4EC] px-3 py-2 font-bold text-[#27364A]">Due date</th>');
  });

  it('starts a clean conversation without losing the persisted history list', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.conversationId.set('conversation-1');
    fixture.componentInstance.messages.set([{ role: 'user', content: 'Previous question' }]);

    fixture.componentInstance.startNewChat();

    expect(fixture.componentInstance.conversationId()).toBeNull();
    expect(fixture.componentInstance.messages()).toEqual([]);
  });

  it('returns a signed-in customer to the customer dashboard', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    const auth = TestBed.inject(AuthService) as unknown as { currentUser: { set: (value: unknown) => void } };
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    auth.currentUser.set({ role: 'Customer' });

    fixture.componentInstance.backToSpeedClaim();

    expect(navigate).toHaveBeenCalledWith('/dashboard');
  });

  it('allows payment only for the earliest unpaid installment', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.paymentSchedules.set([
      { id: 'schedule-2', policyId: 'policy-1', installmentNumber: 2, amountDue: 6800, dueDate: '2026-08-19', status: 'Upcoming' },
      { id: 'schedule-1', policyId: 'policy-1', installmentNumber: 1, amountDue: 6800, dueDate: '2026-07-19', status: 'Upcoming' },
      { id: 'schedule-3', policyId: 'policy-1', installmentNumber: 3, amountDue: 6800, dueDate: '2026-09-19', status: 'Upcoming' },
    ]);

    expect(fixture.componentInstance.canPaySchedule(fixture.componentInstance.paymentSchedules()[1])).toBe(true);
    expect(fixture.componentInstance.canPaySchedule(fixture.componentInstance.paymentSchedules()[0])).toBe(false);
    expect(fixture.componentInstance.paymentAvailabilityMessage(fixture.componentInstance.paymentSchedules()[0]))
      .toBe('Available after installment #1 is paid');
  });

  it('opens and closes the compact section navigator', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);

    fixture.componentInstance.toggleSectionNavigator();
    expect(fixture.componentInstance.sectionNavigatorOpen()).toBe(true);

    fixture.componentInstance.toggleSectionNavigator();
    expect(fixture.componentInstance.sectionNavigatorOpen()).toBe(false);
  });

  it('builds a navigator entry for every user question and moves between entries', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.messages.set([
      { role: 'user', content: 'Which products are available?' },
      { role: 'assistant', content: 'Here are the products.' },
      { role: 'user', content: 'How do I complete KYC?' },
      { role: 'assistant', content: 'Attach Aadhaar and PAN.' },
    ]);

    expect(fixture.componentInstance.conversationSections()).toEqual([
      { messageIndex: 0, label: 'Which products are available?' },
      { messageIndex: 2, label: 'How do I complete KYC?' },
    ]);

    fixture.componentInstance.jumpToMessage(0);
    fixture.componentInstance.moveSection(1);
    expect(fixture.componentInstance.activeSectionIndex()).toBe(2);

    fixture.componentInstance.moveSection(-1);
    expect(fixture.componentInstance.activeSectionIndex()).toBe(0);
  });

  it('uses the regular customer KYC validation messages in the guided composer', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.aadhaarNumber.set('12345678901');
    fixture.componentInstance.panNumber.set('ABCDE1234');

    expect(fixture.componentInstance.aadhaarError()).toBe('Aadhaar must be exactly 12 digits.');
    expect(fixture.componentInstance.panError()).toBe('PAN must be in the format ABCDE1234F.');
    expect(fixture.componentInstance.kycReady()).toBe(false);
  });

  it('does not reopen the KYC composer after both documents have been submitted', () => {
    const fixture = TestBed.createComponent(SpeedyWorkspaceComponent);
    fixture.componentInstance.kycRecord.set({
      id: 'kyc-1', userId: 'user-1', kycStatus: 'Pending', aadhaarUploaded: true, panUploaded: true,
      createdAt: '',
    });

    fixture.componentInstance.runAction({ kind: 'guided_kyc', label: 'Complete KYC', route: null, detail: '', requiresConfirmation: true });

    expect(fixture.componentInstance.showKyc()).toBe(false);
    expect(fixture.componentInstance.messages().at(-1)?.content).toContain('awaiting underwriter review');
  });
});
