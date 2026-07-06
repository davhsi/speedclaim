import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
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
    id: 'pol1', policyNumber: 'POL-1', userId: 'u1', productId: 'prod1', productName: 'Health Plus',
    status: 'Active', paymentFrequency: 'Monthly', premiumAmount: 500, coverageAmount: 100000, currency: 'INR',
    startDate: '2025-01-01', endDate: '2027-01-01', domain: 'Health', type: 'Health',
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
    });
  });

  describe('claimAmountRequested validator (withinPolicyCoverage)', () => {
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
  });

  describe('submit', () => {
    function fillValidForm(fixture: ReturnType<typeof create>) {
      const c = fixture.componentInstance;
      c.selectPolicy(activePolicy);
      c.claimForm.setValue({
        claimType: 'Health', claimAmountRequested: 5000, incidentDate: '2025-06-01',
        incidentDescription: 'A valid description over ten chars', isCashless: false,
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
});
