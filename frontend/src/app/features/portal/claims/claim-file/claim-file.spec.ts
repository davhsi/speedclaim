import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ClaimFileComponent } from './claim-file';
import { ClaimService } from '../services/claim.service';
import { PolicyService } from '../../policies/services/policy.service';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { ClaimDto, PolicyDto } from '../../../../core/models/api.models';

describe('ClaimFileComponent', () => {
  let claimService: { intimate: ReturnType<typeof vi.fn>; uploadDocument: ReturnType<typeof vi.fn> };
  let policyService: { getMyPolicies: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const activePolicy: PolicyDto = {
    id: 'pol1', policyNumber: 'POL-1', customerId: 'u1', productId: 'prod1', productName: 'Health Plus',
    status: 'Active', paymentFrequency: 'Monthly', premiumAmount: 500, coverageAmount: 100000, currency: 'INR',
    startDate: '2025-01-01', endDate: '2027-01-01', domain: 'Health', type: 'Health',
  };
  const futurePolicy: PolicyDto = {
    ...activePolicy,
    id: 'pol-future',
    policyNumber: 'POL-FUTURE',
    startDate: '2999-01-01',
    endDate: '3000-01-01',
  };
  const lifePolicy: PolicyDto = {
    ...activePolicy,
    id: 'pol-life',
    policyNumber: 'POL-LIFE',
    domain: 'Life',
    type: 'Life',
  };
  const motorPolicy: PolicyDto = {
    ...activePolicy,
    id: 'pol-motor',
    policyNumber: 'POL-MOTOR',
    domain: 'Motor',
    type: 'Motor',
  };

  function create(policies: PolicyDto[] = [activePolicy]) {
    policyService.getMyPolicies.mockReturnValue(of(policies));
    TestBed.configureTestingModule({
      imports: [ClaimFileComponent],
      providers: [
        { provide: ClaimService, useValue: claimService },
        { provide: PolicyService, useValue: policyService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
      ],
    });
    const fixture = TestBed.createComponent(ClaimFileComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    claimService = { intimate: vi.fn(), uploadDocument: vi.fn() };
    policyService = { getMyPolicies: vi.fn() };
    toast = { success: vi.fn(), warning: vi.fn(), error: vi.fn() };
    router = { navigate: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads only Active policies', () => {
      const fixture = create();
      expect(policyService.getMyPolicies).toHaveBeenCalledWith('Active');
      expect(fixture.componentInstance.activePolicies()).toEqual([activePolicy]);
      expect(fixture.componentInstance.policiesLoading()).toBe(false);
    });

    it('stops the loading flag when the fetch fails', () => {
      policyService.getMyPolicies.mockReturnValue(throwError(() => ({ status: 500 })));
      TestBed.configureTestingModule({
        imports: [ClaimFileComponent],
        providers: [
          { provide: ClaimService, useValue: claimService },
          { provide: PolicyService, useValue: policyService },
          { provide: ToastService, useValue: toast },
          { provide: Router, useValue: router },
        ],
      });
      const fixture = TestBed.createComponent(ClaimFileComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.policiesLoading()).toBe(false);
    });
  });

  describe('selectPolicy', () => {
    it('sets the policy control and selected policy, and revalidates amount/date fields', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      expect(c.policyControl.value).toBe('pol1');
      expect(c.selectedPolicy()).toEqual(activePolicy);
      expect(c.claimForm.controls.claimType.value).toBe('Health');
    });

    it('does not select a policy whose coverage has not started', () => {
      const fixture = create([futurePolicy]);
      const c = fixture.componentInstance;
      c.selectPolicy(futurePolicy);
      expect(c.policyControl.value).toBe('');
      expect(c.selectedPolicy()).toBeNull();
      expect(c.isPolicyClaimable(futurePolicy)).toBe(false);
      expect(c.policyClaimAvailability(futurePolicy)).toBe('Claims open from');
    });

    it('uses product waiting days when deciding when claims open', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const waitingPolicy: PolicyDto = {
        ...activePolicy,
        startDate: c.today,
        endDate: '2999-01-01',
        waitingPeriodDays: 30,
      };

      expect(c.isPolicyClaimable(waitingPolicy)).toBe(false);
      expect(c.policyClaimAvailability(waitingPolicy)).toBe('Claims open from');
      expect(c.policyClaimAvailabilityDate(waitingPolicy)).not.toBe(waitingPolicy.startDate);
    });

    it('limits claim type options to the selected policy domain', () => {
      const fixture = create([activePolicy, lifePolicy, motorPolicy]);
      const c = fixture.componentInstance;

      c.selectPolicy(activePolicy);
      expect(c.claimTypeOptions().map(o => o.value)).toEqual(['Health']);

      c.selectPolicy(lifePolicy);
      expect(c.claimTypeOptions().map(o => o.value)).toEqual(['Death', 'Maturity']);
      expect(c.claimForm.controls.claimType.value).toBe('Death');

      c.selectPolicy(motorPolicy);
      expect(c.claimTypeOptions().map(o => o.value)).toEqual(['Accident', 'Theft', 'NaturalDamage']);
      expect(c.claimForm.controls.claimType.value).toBe('Accident');
    });
  });

  describe('claimAmountRequested validator (withinPolicyCoverage)', () => {
    it('rejects an amount below the minimum claim amount', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      c.claimForm.controls.claimAmountRequested.setValue(c.minClaimAmount - 1);
      expect(c.claimForm.controls.claimAmountRequested.errors).toEqual(expect.objectContaining({ min: expect.any(Object) }));
    });

    it('rejects an amount above the policy coverage', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      c.claimForm.controls.claimAmountRequested.setValue(200000);
      expect(c.claimForm.controls.claimAmountRequested.errors).toEqual({ aboveCoverage: true });
    });

    it('accepts an amount within the policy coverage', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      c.claimForm.controls.claimAmountRequested.setValue(50000);
      expect(c.claimForm.controls.claimAmountRequested.errors).toBeNull();
    });
  });

  describe('incidentDate validators (notFutureDate / withinPolicyPeriod)', () => {
    it('rejects a future incident date', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      const future = new Date();
      future.setFullYear(future.getFullYear() + 1);
      c.claimForm.controls.incidentDate.setValue(future.toISOString().slice(0, 10));
      expect(c.claimForm.controls.incidentDate.errors).toEqual(expect.objectContaining({ futureDate: true }));
    });

    it('rejects an incident date outside the policy period', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      c.claimForm.controls.incidentDate.setValue('2024-01-01'); // before policy startDate
      expect(c.claimForm.controls.incidentDate.errors).toEqual({ outsidePolicyPeriod: true });
    });

    it('accepts an incident date within the policy period and not in the future', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      c.claimForm.controls.incidentDate.setValue('2025-06-01');
      expect(c.claimForm.controls.incidentDate.errors).toBeNull();
    });
  });

  describe('onFileSelected / onFileRemoved', () => {
    it('tracks a single selected file', () => {
      const fixture = create();
      const file = new File(['x'], 'proof.pdf');
      fixture.componentInstance.onFileSelected(file);
      expect(fixture.componentInstance.uploadedFiles).toEqual([file]);
    });

    it('removes the file on removal', () => {
      const fixture = create();
      const file = new File(['x'], 'proof.pdf');
      fixture.componentInstance.onFileSelected(file);
      fixture.componentInstance.onFileRemoved(file);
      expect(fixture.componentInstance.uploadedFiles).toEqual([]);
    });

    it('appends multiple selected files instead of replacing', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const first = new File(['x'], 'bill.pdf');
      const second = new File(['y'], 'prescription.pdf');
      c.onFileSelected(first);
      c.onFileSelected(second);
      expect(c.uploadedFiles).toEqual([first, second]);
    });

    it('rejects new files once the max document cap is reached', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      for (let i = 0; i < c.maxDocuments; i++) {
        c.onFileSelected(new File(['x'], `doc${i}.pdf`));
      }
      c.onFileSelected(new File(['x'], 'onemore.pdf'));
      expect(c.uploadedFiles).toHaveLength(c.maxDocuments);
      expect(toast.warning).toHaveBeenCalledWith(`You can attach up to ${c.maxDocuments} documents`);
    });
  });

  describe('submit', () => {
    function fillValidForm(fixture: ReturnType<typeof create>) {
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      c.claimForm.setValue({
        claimType: 'Health', claimAmountRequested: 5000, incidentDate: '2025-06-01',
        incidentDescription: 'A valid description over ten chars',
      });
      return c;
    }

    it('does nothing when no policy is selected or the form is invalid', () => {
      const fixture = create();
      fixture.componentInstance.submit();
      expect(claimService.intimate).not.toHaveBeenCalled();
    });

    it('intimates the claim and navigates without uploading when no files are attached', () => {
      const fixture = create();
      const c = fillValidForm(fixture);
      claimService.intimate.mockReturnValue(of({ id: 'claim1' } as ClaimDto));

      c.submit();

      expect(claimService.intimate).toHaveBeenCalledWith(expect.objectContaining({ policyId: 'pol1', claimType: 'Health', claimAmountRequested: 5000 }));
      expect(toast.success).toHaveBeenCalledWith('Claim filed successfully');
      expect(router.navigate).toHaveBeenCalledWith(['/claims', 'claim1']);
    });

    it('uploads attached documents after intimation succeeds, then navigates', () => {
      const fixture = create();
      const c = fillValidForm(fixture);
      claimService.intimate.mockReturnValue(of({ id: 'claim1' } as ClaimDto));
      claimService.uploadDocument.mockReturnValue(of({ message: 'ok' }));
      c.onFileSelected(new File(['x'], 'proof.pdf'));

      c.submit();

      expect(claimService.uploadDocument).toHaveBeenCalledWith('claim1', 'PROOF', expect.any(File));
      expect(toast.success).toHaveBeenCalledWith('Claim filed successfully');
      expect(router.navigate).toHaveBeenCalledWith(['/claims', 'claim1']);
    });

    it('uploads multiple documents sequentially, not in parallel', () => {
      // Concurrent uploads would each read the claim's pre-transition status server-side
      // and duplicate the Intimated -> UnderReview status change once per file.
      const fixture = create();
      const c = fillValidForm(fixture);
      claimService.intimate.mockReturnValue(of({ id: 'claim1' } as ClaimDto));
      const callOrder: string[] = [];
      claimService.uploadDocument.mockImplementation((_claimId: string, key: string) => {
        callOrder.push(`start:${key}`);
        return of({ message: 'ok' }).pipe(tap(() => callOrder.push(`end:${key}`)));
      });
      c.onFileSelected(new File(['x'], 'bill.pdf'));
      c.onFileSelected(new File(['y'], 'prescription.pdf'));

      c.submit();

      expect(callOrder).toEqual(['start:BILL', 'end:BILL', 'start:PRESCRIPTION', 'end:PRESCRIPTION']);
    });

    it('dedupes document keys when two attached files share the same name', () => {
      const fixture = create();
      const c = fillValidForm(fixture);
      claimService.intimate.mockReturnValue(of({ id: 'claim1' } as ClaimDto));
      claimService.uploadDocument.mockReturnValue(of({ message: 'ok' }));
      c.onFileSelected(new File(['x'], 'proof.pdf'));
      c.onFileSelected(new File(['y'], 'proof.pdf'));

      c.submit();

      expect(claimService.uploadDocument).toHaveBeenCalledWith('claim1', 'PROOF', expect.any(File));
      expect(claimService.uploadDocument).toHaveBeenCalledWith('claim1', 'PROOF_2', expect.any(File));
    });

    it('warns but still navigates when a document upload fails', () => {
      const fixture = create();
      const c = fillValidForm(fixture);
      claimService.intimate.mockReturnValue(of({ id: 'claim1' } as ClaimDto));
      claimService.uploadDocument.mockReturnValue(throwError(() => ({ status: 500 })));
      c.onFileSelected(new File(['x'], 'proof.pdf'));

      c.submit();

      expect(toast.warning).toHaveBeenCalledWith('Claim filed but some documents failed to upload');
      expect(router.navigate).toHaveBeenCalledWith(['/claims', 'claim1']);
    });

    it('shows an error and resets submitting when intimation fails', () => {
      const fixture = create();
      const c = fillValidForm(fixture);
      claimService.intimate.mockReturnValue(throwError(() => ({ status: 500 })));

      c.submit();

      expect(toast.error).toHaveBeenCalledWith('Failed to file claim. Please try again.');
      expect(c.submitting()).toBe(false);
    });
  });

  describe('canDeactivate', () => {
    it('allows navigation when nothing has been touched', () => {
      const fixture = create();
      expect(fixture.componentInstance.canDeactivate()).toBe(true);
    });

    it('prompts for confirmation once the claim form has been edited', () => {
      const fixture = create();
      fixture.componentInstance.claimForm.markAsDirty();

      const result = fixture.componentInstance.canDeactivate();

      expect(fixture.componentInstance.showLeaveConfirm()).toBe(true);
      expect(result).not.toBe(true);
    });

    it('prompts for confirmation when a file has been attached', () => {
      const fixture = create();
      fixture.componentInstance.onFileSelected(new File(['x'], 'proof.pdf'));

      expect(fixture.componentInstance.canDeactivate()).not.toBe(true);
    });

    it('resolves true on confirmLeave and false on cancelLeave', async () => {
      const fixture = create();
      fixture.componentInstance.claimForm.markAsDirty();

      const result$ = fixture.componentInstance.canDeactivate();
      const resultPromise = new Promise(resolve => (result$ as any).subscribe(resolve));
      fixture.componentInstance.confirmLeave();

      expect(await resultPromise).toBe(true);
      expect(fixture.componentInstance.showLeaveConfirm()).toBe(false);
    });

    it('allows navigation without prompting right after a successful submit', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      c.claimForm.setValue({
        claimType: 'Health', claimAmountRequested: 5000, incidentDate: '2025-06-01',
        incidentDescription: 'A valid description over ten chars',
      });
      claimService.intimate.mockReturnValue(of({ id: 'claim1' } as ClaimDto));

      c.submit();

      expect(c.canDeactivate()).toBe(true);
    });
  });
});
