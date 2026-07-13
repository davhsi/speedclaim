import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AgentService, RenewalReminderDto } from '../services/agent.service';
import { AgentRenewalListComponent } from './renewal-list';

describe('AgentRenewalListComponent', () => {
  let agentService: { getRenewals: ReturnType<typeof vi.fn>; sendRenewalReminder: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn> };

  const renewal = (overrides: Partial<RenewalReminderDto> = {}): RenewalReminderDto => ({
    policyId: 'pol1',
    policyNumber: 'POL-1',
    customerId: 'cust1',
    customerName: 'Ms Dharsini K',
    customerEmail: 'customer@test.com',
    customerPhone: '9876543210',
    dueDate: '2026-07-20',
    amountDue: 22000,
    daysUntilDue: 6,
    reminderSentRecently: false,
    ...overrides,
  });

  function create(renewals: RenewalReminderDto[] = [renewal()]) {
    TestBed.resetTestingModule();
    agentService.getRenewals.mockReturnValue(of(renewals));
    TestBed.configureTestingModule({
      imports: [AgentRenewalListComponent],
      providers: [
        { provide: AgentService, useValue: agentService },
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: toast },
      ],
    });
    const fixture = TestBed.createComponent(AgentRenewalListComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    agentService = { getRenewals: vi.fn(), sendRenewalReminder: vi.fn() };
    router = { navigate: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };
  });

  it('sends a renewal reminder through the backend', () => {
    const fixture = create();
    agentService.sendRenewalReminder.mockReturnValue(of({ message: 'sent' }));

    fixture.componentInstance.sendReminder(renewal());

    expect(agentService.sendRenewalReminder).toHaveBeenCalledWith('pol1');
    expect(toast.success).toHaveBeenCalledWith('Premium reminder sent to the customer.');
    expect(fixture.componentInstance.sentReminderIds().has('pol1')).toBe(true);
    expect(fixture.componentInstance.sendingReminderId()).toBeNull();
  });

  it('uses backend reminder state after loading renewals', () => {
    const fixture = create([renewal({ reminderSentRecently: true })]);

    expect(fixture.componentInstance.sentReminderIds().has('pol1')).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Reminder sent');
  });

  it('shows an error when the backend reminder request fails', () => {
    const fixture = create();
    agentService.sendRenewalReminder.mockReturnValue(throwError(() => ({ status: 500 })));

    fixture.componentInstance.sendReminder(renewal());

    expect(toast.error).toHaveBeenCalledWith('Could not send premium reminder.');
    expect(fixture.componentInstance.sendingReminderId()).toBeNull();
  });

  it('marks the reminder sent when the backend reports a recent duplicate', () => {
    const fixture = create();
    agentService.sendRenewalReminder.mockReturnValue(throwError(() => ({ status: 409 })));

    fixture.componentInstance.sendReminder(renewal());

    expect(toast.warning).toHaveBeenCalledWith('A reminder was already sent for this policy in the last 24 hours.');
    expect(fixture.componentInstance.sentReminderIds().has('pol1')).toBe(true);
  });

  it('navigates to the highlighted policy from view details', () => {
    const fixture = create();

    fixture.componentInstance.viewDetails(renewal());

    expect(router.navigate).toHaveBeenCalledWith(['/agent/policies'], { queryParams: { policyId: 'pol1' } });
  });
});
