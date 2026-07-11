import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { RegisterComponent } from './register';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

describe('RegisterComponent', () => {
  let authService: { register: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn> };

  const address = { line1: '123 St', line2: '', city: 'Mumbai', state: 'Maharashtra', postalCode: '400001', country: 'India' };
  const otherAddress = { line1: '456 Ave', line2: '', city: 'Pune', state: 'Maharashtra', postalCode: '411001', country: 'India' };

  function create() {
    const fixture = TestBed.createComponent(RegisterComponent);
    fixture.detectChanges();
    return fixture;
  }

  function fillStep1(fixture: ReturnType<typeof create>) {
    fixture.componentInstance.form.patchValue({
      salutationTitle: 'Mr',
      firstName: 'Jane',
      lastName: 'Doe',
      email: 'jane@example.com',
      phone: '9876543210',
      dateOfBirth: '1990-01-01',
      gender: 'Female',
      maritalStatus: 'Single',
    });
  }

  function fillStep2(fixture: ReturnType<typeof create>) {
    fixture.componentInstance.form.patchValue({
      password: 'Secret123!',
      confirmPassword: 'Secret123!',
    });
  }

  function fillStep3(fixture: ReturnType<typeof create>) {
    fixture.componentInstance.form.patchValue({ permanentAddress: address, currentAddress: otherAddress });
  }

  function fillStep4(fixture: ReturnType<typeof create>) {
    fixture.componentInstance.form.patchValue({ consentDataProcessing: true, consentKycCollection: true });
  }

  function fillAllStepsAndGoToLast(fixture: ReturnType<typeof create>) {
    fillStep1(fixture);
    fillStep2(fixture);
    fillStep3(fixture);
    fillStep4(fixture);
    fixture.componentInstance.currentStep.set(4);
  }

  beforeEach(() => {
    authService = { register: vi.fn() };
    router = { navigate: vi.fn() };
    toast = { success: vi.fn() };

    TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: {} },
      ],
    });
  });

  describe('nextStep / prevStep', () => {
    it('does not advance and touches the fields when the current step is invalid', () => {
      const fixture = create();
      fixture.componentInstance.nextStep();
      expect(fixture.componentInstance.currentStep()).toBe(1);
      expect(fixture.componentInstance.form.controls.firstName.touched).toBe(true);
    });

    it('advances to the next step once the current step is valid', () => {
      const fixture = create();
      fillStep1(fixture);
      fixture.componentInstance.nextStep();
      expect(fixture.componentInstance.currentStep()).toBe(2);
    });

    it('clears any error message when navigating back', () => {
      const fixture = create();
      fillStep1(fixture);
      fixture.componentInstance.nextStep();
      fixture.componentInstance.errorMessage.set('some previous error');

      fixture.componentInstance.prevStep();

      expect(fixture.componentInstance.currentStep()).toBe(1);
      expect(fixture.componentInstance.errorMessage()).toBe('');
    });
  });

  describe('toggleSameAddress', () => {
    it('copies the permanent address into currentAddress and disables it when checked', () => {
      const fixture = create();
      fillStep3(fixture);

      fixture.componentInstance.toggleSameAddress();

      expect(fixture.componentInstance.sameAsPermanent()).toBe(true);
      expect(fixture.componentInstance.form.controls.currentAddress.getRawValue()).toEqual(address);
      expect(fixture.componentInstance.form.controls.currentAddress.disabled).toBe(true);
    });

    it('re-enables currentAddress when unchecked again', () => {
      const fixture = create();
      fillStep3(fixture);
      fixture.componentInstance.toggleSameAddress();
      fixture.componentInstance.toggleSameAddress();

      expect(fixture.componentInstance.sameAsPermanent()).toBe(false);
      expect(fixture.componentInstance.form.controls.currentAddress.disabled).toBe(false);
    });
  });

  describe('isStepValid (address step, step 3)', () => {
    it('is invalid when currentAddress is blank and enabled', () => {
      const fixture = create();
      fixture.componentInstance.form.patchValue({ permanentAddress: address });
      expect(fixture.componentInstance.isStepValid(3)).toBe(false);
    });

    it('treats a disabled currentAddress (same-as-permanent) as satisfying the step', () => {
      const fixture = create();
      fixture.componentInstance.form.patchValue({ permanentAddress: address });
      fixture.componentInstance.toggleSameAddress();
      expect(fixture.componentInstance.isStepValid(3)).toBe(true);
    });
  });

  describe('onSubmit', () => {
    it('does not submit when the current (last-visited) step is invalid', () => {
      const fixture = create();
      fixture.componentInstance.onSubmit();
      expect(authService.register).not.toHaveBeenCalled();
    });

    it('submits the mapped payload with distinct addresses when not using same-as-permanent', () => {
      const fixture = create();
      fillAllStepsAndGoToLast(fixture);
      authService.register.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onSubmit();

      expect(authService.register).toHaveBeenCalledWith(expect.objectContaining({
        email: 'jane@example.com',
        salutation: 'Mr',
        firstName: 'Jane',
        lastName: 'Doe',
        phone: '9876543210',
        permanentAddress: address,
        currentAddress: otherAddress,
        isSameAsPermanent: false,
      }));
    });

    it('mirrors the permanent address into currentAddress when same-as-permanent is checked', () => {
      const fixture = create();
      fillAllStepsAndGoToLast(fixture);
      fixture.componentInstance.toggleSameAddress();
      authService.register.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onSubmit();

      const payload = authService.register.mock.calls[0][0];
      expect(payload.currentAddress).toEqual(address);
      expect(payload.isSameAsPermanent).toBe(true);
    });

    it('shows a success toast and navigates to login on success', () => {
      const fixture = create();
      fillAllStepsAndGoToLast(fixture);
      authService.register.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onSubmit();

      expect(toast.success).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('shows a loading state while in flight, blocks a duplicate submit, and clears on success', () => {
      const fixture = create();
      fillAllStepsAndGoToLast(fixture);
      const request$ = new Subject<{ message: string }>();
      authService.register.mockReturnValue(request$);

      fixture.componentInstance.onSubmit();
      expect(fixture.componentInstance.loading()).toBe(true);

      fixture.componentInstance.onSubmit();
      expect(authService.register).toHaveBeenCalledTimes(1);

      request$.next({ message: 'ok' });
      request$.complete();

      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('clears the loading state on failure so the form can be retried', () => {
      const fixture = create();
      fillAllStepsAndGoToLast(fixture);
      const request$ = new Subject<{ message: string }>();
      authService.register.mockReturnValue(request$);

      fixture.componentInstance.onSubmit();
      request$.error({ status: 500, error: {} });

      expect(fixture.componentInstance.loading()).toBe(false);
    });

    describe('error message mapping', () => {
      function submitWithError(fixture: ReturnType<typeof create>, error: unknown) {
        fillAllStepsAndGoToLast(fixture);
        authService.register.mockReturnValue(throwError(() => error));
        fixture.componentInstance.onSubmit();
        return fixture.componentInstance.errorMessage();
      }

      it('prefers err.error.detail', () => {
        const fixture = create();
        expect(submitWithError(fixture, { error: { detail: 'Duplicate PAN' } })).toBe('Duplicate PAN');
      });

      it('joins validation errors from err.error.errors', () => {
        const fixture = create();
        const msg = submitWithError(fixture, { error: { errors: { Email: ['Email already in use'], Phone: ['Invalid phone'] } } });
        expect(msg).toBe('Email already in use Invalid phone');
      });

      it('falls back to err.error.message', () => {
        const fixture = create();
        expect(submitWithError(fixture, { error: { message: 'Server said no' } })).toBe('Server said no');
      });

      it('maps a bare 409 to a duplicate-registration message', () => {
        const fixture = create();
        expect(submitWithError(fixture, { status: 409, error: {} })).toBe('This information is already registered.');
      });

      it('maps a bare 400 to a generic validation message', () => {
        const fixture = create();
        expect(submitWithError(fixture, { status: 400, error: {} })).toBe('Please check your input and try again.');
      });

      it('maps a 5xx to a generic server-error message', () => {
        const fixture = create();
        expect(submitWithError(fixture, { status: 500, error: {} })).toBe('Something went wrong. Please try again later.');
      });

      it('falls back to a generic registration-failed message for anything else', () => {
        const fixture = create();
        expect(submitWithError(fixture, { status: 418, error: {} })).toBe('Registration failed. Please try again.');
      });
    });
  });

  describe('canDeactivate', () => {
    it('allows navigation when the form is untouched', () => {
      const fixture = create();
      expect(fixture.componentInstance.canDeactivate()).toBe(true);
    });

    it('allows navigation when registration completed successfully, even if the form is dirty', () => {
      const fixture = create();
      fixture.componentInstance.form.markAsDirty();
      fillAllStepsAndGoToLast(fixture);
      authService.register.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.onSubmit();

      expect(fixture.componentInstance.canDeactivate()).toBe(true);
    });

    it('prompts for confirmation when the form is dirty and unsubmitted, resolving true on confirm', async () => {
      const fixture = create();
      fixture.componentInstance.form.markAsDirty();

      const result$ = fixture.componentInstance.canDeactivate();
      expect(fixture.componentInstance.showLeaveConfirm()).toBe(true);
      expect(result$).not.toBe(true);

      const resultPromise = new Promise(resolve => (result$ as any).subscribe(resolve));
      fixture.componentInstance.confirmLeave();

      expect(await resultPromise).toBe(true);
      expect(fixture.componentInstance.showLeaveConfirm()).toBe(false);
    });

    it('prompts for confirmation when the form is dirty and unsubmitted, resolving false on cancel', async () => {
      const fixture = create();
      fixture.componentInstance.form.markAsDirty();

      const result$ = fixture.componentInstance.canDeactivate();
      const resultPromise = new Promise(resolve => (result$ as any).subscribe(resolve));
      fixture.componentInstance.cancelLeave();

      expect(await resultPromise).toBe(false);
      expect(fixture.componentInstance.showLeaveConfirm()).toBe(false);
    });
  });
});
