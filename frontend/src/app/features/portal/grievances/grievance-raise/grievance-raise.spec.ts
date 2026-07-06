import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GrievanceRaiseComponent } from './grievance-raise';
import { GrievanceService } from '../services/grievance.service';
import { PolicyService } from '../../policies/services/policy.service';
import { ClaimService } from '../../claims/services/claim.service';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { ClaimDto, GrievanceDto, PolicyDto } from '../../../../core/models/api.models';

describe('GrievanceRaiseComponent', () => {
  let grievanceService: { raise: ReturnType<typeof vi.fn>; uploadAttachment: ReturnType<typeof vi.fn> };
  let policyService: { getMyPolicies: ReturnType<typeof vi.fn> };
  let claimService: { getMyClaims: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  function create() {
    const fixture = TestBed.createComponent(GrievanceRaiseComponent);
    fixture.detectChanges();
    return fixture;
  }

  function fillValidForm(fixture: ReturnType<typeof create>) {
    fixture.componentInstance.form.setValue({
      category: 'ClaimDelay', policyId: '', claimId: '', description: 'This is a long enough description.',
    });
  }

  beforeEach(() => {
    grievanceService = { raise: vi.fn(), uploadAttachment: vi.fn() };
    policyService = { getMyPolicies: vi.fn(() => of([{ id: 'pol1' } as PolicyDto])) };
    claimService = { getMyClaims: vi.fn(() => of([{ id: 'claim1' } as ClaimDto])) };
    toast = { success: vi.fn(), error: vi.fn() };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      imports: [GrievanceRaiseComponent],
      providers: [
        { provide: GrievanceService, useValue: grievanceService },
        { provide: PolicyService, useValue: policyService },
        { provide: ClaimService, useValue: claimService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
      ],
    });
  });

  describe('ngOnInit', () => {
    it('loads the policies and claims dropdown lists', () => {
      const fixture = create();
      expect(fixture.componentInstance.policies()).toEqual([{ id: 'pol1' }]);
      expect(fixture.componentInstance.claims()).toEqual([{ id: 'claim1' }]);
    });
  });

  describe('onFileSelected', () => {
    it('stores the selected file', () => {
      const fixture = create();
      const file = new File(['x'], 'proof.png');
      fixture.componentInstance.onFileSelected(file);
      expect(fixture.componentInstance.attachedFile()).toBe(file);
    });
  });

  describe('submit', () => {
    it('does nothing when the form is invalid', () => {
      const fixture = create();
      fixture.componentInstance.submit();
      expect(grievanceService.raise).not.toHaveBeenCalled();
    });

    it('does nothing while already submitting', () => {
      const fixture = create();
      fillValidForm(fixture);
      fixture.componentInstance.submitting.set(true);
      fixture.componentInstance.submit();
      expect(grievanceService.raise).not.toHaveBeenCalled();
    });

    it('maps blank policyId/claimId to undefined and submits without a file', () => {
      const fixture = create();
      fillValidForm(fixture);
      grievanceService.raise.mockReturnValue(of({ id: 'g1' } as GrievanceDto));

      fixture.componentInstance.submit();

      expect(grievanceService.raise).toHaveBeenCalledWith({
        category: 'ClaimDelay', description: 'This is a long enough description.', policyId: undefined, claimId: undefined,
      });
      expect(grievanceService.uploadAttachment).not.toHaveBeenCalled();
      expect(toast.success).toHaveBeenCalledWith('Grievance submitted');
      expect(router.navigate).toHaveBeenCalledWith(['/grievances']);
    });

    it('uploads the attached file after a successful raise, and reports full success', () => {
      const fixture = create();
      fillValidForm(fixture);
      const file = new File(['x'], 'proof.png');
      fixture.componentInstance.onFileSelected(file);
      grievanceService.raise.mockReturnValue(of({ id: 'g1' } as GrievanceDto));
      grievanceService.uploadAttachment.mockReturnValue(of({ filePath: 'x' }));

      fixture.componentInstance.submit();

      expect(grievanceService.uploadAttachment).toHaveBeenCalledWith('g1', file);
      expect(toast.success).toHaveBeenCalledWith('Grievance submitted with attachment');
      expect(router.navigate).toHaveBeenCalledWith(['/grievances']);
    });

    it('still reports success (with a caveat) and navigates when only the attachment upload fails', () => {
      const fixture = create();
      fillValidForm(fixture);
      fixture.componentInstance.onFileSelected(new File(['x'], 'proof.png'));
      grievanceService.raise.mockReturnValue(of({ id: 'g1' } as GrievanceDto));
      grievanceService.uploadAttachment.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.submit();

      expect(toast.success).toHaveBeenCalledWith('Grievance submitted (attachment upload failed)');
      expect(router.navigate).toHaveBeenCalledWith(['/grievances']);
    });

    it('resets submitting and shows an error toast when raising the grievance fails', () => {
      const fixture = create();
      fillValidForm(fixture);
      grievanceService.raise.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.submit();

      expect(fixture.componentInstance.submitting()).toBe(false);
      expect(toast.error).toHaveBeenCalledWith('Submission failed');
      expect(router.navigate).not.toHaveBeenCalled();
    });
  });
});
