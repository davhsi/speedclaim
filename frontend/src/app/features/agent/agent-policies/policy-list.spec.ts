import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { PolicyDto } from '../../../core/models/api.models';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AgentService } from '../services/agent.service';
import { AgentPolicyListComponent } from './policy-list';

describe('AgentPolicyListComponent', () => {
  let agentService: { getAssignedPolicies: ReturnType<typeof vi.fn>; remindCustomerToPay: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn> };

  const policy = (overrides: Partial<PolicyDto> = {}): PolicyDto => ({
    id: 'pol1',
    policyNumber: 'POL-1',
    customerId: 'cust1',
    productId: 'prod1',
    productName: 'SpeedDrive Comprehensive Motor',
    status: 'Pending',
    paymentFrequency: 'Annually',
    premiumAmount: 22000,
    coverageAmount: 800000,
    currency: 'INR',
    startDate: '2026-07-13',
    endDate: '2027-07-20',
    domain: 'Motor',
    type: 'Individual',
    ...overrides,
  });

  function create(policies: PolicyDto[] = [policy()]) {
    TestBed.resetTestingModule();
    agentService.getAssignedPolicies.mockReturnValue(of(policies));
    TestBed.configureTestingModule({
      imports: [AgentPolicyListComponent],
      providers: [
        { provide: AgentService, useValue: agentService },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    });
    const fixture = TestBed.createComponent(AgentPolicyListComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    agentService = { getAssignedPolicies: vi.fn(), remindCustomerToPay: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };
  });

  it('shows payment reminder actions only for pending policies', () => {
    const fixture = create([policy(), policy({ id: 'pol2', policyNumber: 'POL-2', status: 'Active' })]);

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Notify customer');
    expect(text).toContain('Copy instructions');
    expect(fixture.componentInstance.canRequestPayment(policy({ status: 'Pending' }))).toBe(true);
    expect(fixture.componentInstance.canRequestPayment(policy({ status: 'Active' }))).toBe(false);
  });

  it('sends the payment reminder and shows success', () => {
    const fixture = create();
    agentService.remindCustomerToPay.mockReturnValue(of({ message: 'sent' }));

    fixture.componentInstance.notifyCustomer(policy());

    expect(agentService.remindCustomerToPay).toHaveBeenCalledWith('pol1');
    expect(toast.success).toHaveBeenCalledWith('Payment reminder sent to the customer.');
    expect(fixture.componentInstance.remindedPolicyIds().has('pol1')).toBe(true);
    expect(fixture.componentInstance.remindingPolicyId()).toBeNull();
  });

  it('shows an error when the payment reminder fails', () => {
    const fixture = create();
    agentService.remindCustomerToPay.mockReturnValue(throwError(() => ({ status: 500 })));

    fixture.componentInstance.notifyCustomer(policy());

    expect(toast.error).toHaveBeenCalledWith('Could not send payment reminder.');
    expect(fixture.componentInstance.remindingPolicyId()).toBeNull();
  });

  it('marks the payment reminder sent when the backend reports a recent duplicate', () => {
    const fixture = create();
    agentService.remindCustomerToPay.mockReturnValue(throwError(() => ({ status: 409 })));

    fixture.componentInstance.notifyCustomer(policy());

    expect(toast.warning).toHaveBeenCalledWith('A payment reminder was already sent for this policy in the last 24 hours.');
    expect(fixture.componentInstance.remindedPolicyIds().has('pol1')).toBe(true);
  });

  it('copies customer payment instructions', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(globalThis.navigator, 'clipboard', {
      configurable: true,
      value: { writeText },
    });
    const fixture = create();

    await fixture.componentInstance.copyPaymentInstructions(policy());

    expect(writeText).toHaveBeenCalledWith(expect.stringContaining('/pay/pol1'));
    expect(writeText).toHaveBeenCalledWith(expect.stringContaining('POL-1'));
    expect(toast.success).toHaveBeenCalledWith('Payment instructions copied.');
  });
});
