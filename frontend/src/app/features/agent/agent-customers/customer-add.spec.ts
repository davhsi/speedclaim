import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { AgentCustomerAddComponent } from './customer-add';
import { AgentService } from '../services/agent.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

describe('AgentCustomerAddComponent', () => {
  let agentService: { addCustomer: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn> };

  const address = { line1: '123 St', line2: '', city: 'Mumbai', state: 'Maharashtra', postalCode: '400001', country: 'India' };
  const otherAddress = { line1: '456 Ave', line2: '', city: 'Pune', state: 'Maharashtra', postalCode: '411001', country: 'India' };

  function create() {
    const fixture = TestBed.createComponent(AgentCustomerAddComponent);
    fixture.detectChanges();
    return fixture;
  }

  function fillForm(fixture: ReturnType<typeof create>) {
    fixture.componentInstance.form.patchValue({
      salutationTitle: 'Mr',
      firstName: 'Rahul',
      lastName: 'Verma',
      email: 'rahul@example.com',
      phone: '9876543210',
      dateOfBirth: '1990-01-01',
      gender: 'Male',
      maritalStatus: 'Single',
      occupation: 'Software Engineer',
      annualIncome: 600000,
      permanentAddress: address,
      currentAddress: otherAddress,
    });
  }

  beforeEach(() => {
    agentService = { addCustomer: vi.fn() };
    router = { navigate: vi.fn() };
    toast = { success: vi.fn() };

    TestBed.configureTestingModule({
      imports: [AgentCustomerAddComponent],
      providers: [
        { provide: AgentService, useValue: agentService },
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: {} },
      ],
    });
  });

  describe('onSubmit', () => {
    it('does not submit an invalid form', () => {
      const fixture = create();
      fixture.componentInstance.onSubmit();
      expect(agentService.addCustomer).not.toHaveBeenCalled();
    });

    it('submits the mapped payload and navigates on success', () => {
      const fixture = create();
      fillForm(fixture);
      agentService.addCustomer.mockReturnValue(of({ email: 'rahul@example.com', role: 'Customer' }));

      fixture.componentInstance.onSubmit();

      expect(agentService.addCustomer).toHaveBeenCalledWith(expect.objectContaining({
        email: 'rahul@example.com',
        salutation: 'Mr',
        firstName: 'Rahul',
        lastName: 'Verma',
        phone: '9876543210',
        dateOfBirth: '1990-01-01',
        gender: 'Male',
        maritalStatus: 'Single',
        occupation: 'Software Engineer',
        annualIncome: 600000,
        permanentAddress: address,
        currentAddress: otherAddress,
        isSameAsPermanent: false,
      }));
      expect(toast.success).toHaveBeenCalledWith(expect.stringContaining('Customer added'));
      expect(router.navigate).toHaveBeenCalledWith(['/agent/customers']);
    });

    it('requires occupation and annual income before submitting', () => {
      const fixture = create();
      fillForm(fixture);
      fixture.componentInstance.form.patchValue({ occupation: '', annualIncome: null });

      fixture.componentInstance.onSubmit();

      expect(agentService.addCustomer).not.toHaveBeenCalled();
      expect(fixture.componentInstance.form.controls.occupation.errors?.['required']).toBeTruthy();
      expect(fixture.componentInstance.form.controls.annualIncome.errors?.['required']).toBeTruthy();
    });

    it('uses the permanent address as current when same-as-permanent is checked', () => {
      const fixture = create();
      fillForm(fixture);
      fixture.componentInstance.toggleSameAddress();
      agentService.addCustomer.mockReturnValue(of({ email: 'rahul@example.com', role: 'Customer' }));

      fixture.componentInstance.onSubmit();

      expect(agentService.addCustomer).toHaveBeenCalledWith(expect.objectContaining({
        currentAddress: address,
        isSameAsPermanent: true,
      }));
    });

    it('shows an error message when submission fails', () => {
      const fixture = create();
      fillForm(fixture);
      agentService.addCustomer.mockReturnValue(throwError(() => ({ status: 409 })));

      fixture.componentInstance.onSubmit();

      expect(fixture.componentInstance.errorMessage()).toBe('This information is already registered.');
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('sets loading while in flight, blocks a duplicate submit, and clears on success', () => {
      const fixture = create();
      fillForm(fixture);
      const subject = new Subject<any>();
      agentService.addCustomer.mockReturnValue(subject);

      fixture.componentInstance.onSubmit();

      expect(fixture.componentInstance.loading()).toBe(true);
      fixture.componentInstance.onSubmit();
      expect(agentService.addCustomer).toHaveBeenCalledTimes(1);

      subject.next({ email: 'rahul@example.com', role: 'Customer' });
      subject.complete();

      expect(fixture.componentInstance.loading()).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/agent/customers']);
    });

    it('clears loading on error', () => {
      const fixture = create();
      fillForm(fixture);
      const subject = new Subject<any>();
      agentService.addCustomer.mockReturnValue(subject);

      fixture.componentInstance.onSubmit();
      expect(fixture.componentInstance.loading()).toBe(true);

      subject.error({ status: 500 });

      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('toggleSameAddress', () => {
    it('copies the permanent address into current and disables it', () => {
      const fixture = create();
      fillForm(fixture);

      fixture.componentInstance.toggleSameAddress();

      expect(fixture.componentInstance.sameAsPermanent()).toBe(true);
      expect(fixture.componentInstance.form.controls.currentAddress.disabled).toBe(true);
      expect(fixture.componentInstance.form.controls.currentAddress.getRawValue()).toEqual(address);
    });

    it('re-enables current address when toggled off', () => {
      const fixture = create();
      fixture.componentInstance.toggleSameAddress();
      fixture.componentInstance.toggleSameAddress();

      expect(fixture.componentInstance.sameAsPermanent()).toBe(false);
      expect(fixture.componentInstance.form.controls.currentAddress.disabled).toBe(false);
    });
  });
});
