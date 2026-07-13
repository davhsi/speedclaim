import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subject, of, throwError } from 'rxjs';
import { ProposalSubmitComponent } from './proposal-submit';
import { ProposalService } from '../services/proposal.service';
import { ProfileService } from '../../profile/services/profile.service';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { DocumentRequirementDto, FamilyMemberDto, ProposalDto, UserDto } from '../../../../core/models/api.models';

describe('ProposalSubmitComponent', () => {
  let proposalService: { submit: ReturnType<typeof vi.fn>; uploadDocument: ReturnType<typeof vi.fn> };
  let profileService: { getProfile: ReturnType<typeof vi.fn>; getFamilyMembers: ReturnType<typeof vi.fn> };
  let http: { get: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const quoteState = { productId: 'prod1', sumAssured: 100000, tenureYears: 10, premiumAmount: 500, paymentFrequency: 'Monthly' };
  const lifeQuoteState = { ...quoteState, domain: 'Life' };
  const motorQuoteState = {
    ...quoteState,
    domain: 'Motor',
    motorVehicleType: 'TwoWheeler',
    motorDetail: { vehicleMake: 'TVS', vehicleModel: 'Jupiter', manufactureYear: 2023, insuredDeclaredValue: 75000 },
  };
  const docRequirements: DocumentRequirementDto[] = [
    { id: 'd1', documentKey: 'ID_PROOF', label: 'ID Proof', description: '', isMandatory: true },
    { id: 'd2', documentKey: 'OPTIONAL_DOC', label: 'Optional', description: '', isMandatory: false },
  ];

  function withHistoryState(state: Record<string, unknown> | null) {
    history.replaceState(state, '');
  }

  function create({
    profile = { customerId: 'cust1' } as UserDto,
    familyMembers = [{ id: 'm1' } as FamilyMemberDto],
    requirements = docRequirements,
    requirementsError = false,
  }: {
    profile?: UserDto;
    familyMembers?: FamilyMemberDto[];
    requirements?: DocumentRequirementDto[];
    requirementsError?: boolean;
  } = {}) {
    profileService.getProfile.mockReturnValue(of({ customerId: 'cust1' } as UserDto));
    profileService.getProfile.mockReturnValue(of(profile));
    profileService.getFamilyMembers.mockReturnValue(of(familyMembers));
    http.get.mockReturnValue(requirementsError ? throwError(() => ({ status: 500 })) : of(requirements));

    TestBed.configureTestingModule({
      imports: [ProposalSubmitComponent],
      providers: [
        { provide: ProposalService, useValue: proposalService },
        { provide: ProfileService, useValue: profileService },
        { provide: HttpClient, useValue: http },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {} },
      ],
    });
    const fixture = TestBed.createComponent(ProposalSubmitComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    proposalService = { submit: vi.fn(), uploadDocument: vi.fn() };
    profileService = { getProfile: vi.fn(), getFamilyMembers: vi.fn() };
    http = { get: vi.fn() };
    toast = { success: vi.fn(), warning: vi.fn(), error: vi.fn() };
    router = { navigate: vi.fn() };
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-07-06T00:00:00'));
  });

  afterEach(() => {
    vi.useRealTimers();
    history.replaceState(null, '');
  });

  describe('ngOnInit', () => {
    it('redirects to /quote when no quote state was passed via router navigation', () => {
      withHistoryState(null);
      const fixture = create();
      expect(toast.warning).toHaveBeenCalledWith('Please generate a quote before submitting a proposal.');
      expect(router.navigate).toHaveBeenCalledWith(['/quote'], { replaceUrl: true });
      expect(profileService.getProfile).not.toHaveBeenCalled();
    });

    it('patches and locks the quote-derived fields, then loads profile/family/doc requirements', () => {
      withHistoryState(quoteState);
      const fixture = create();
      const c = fixture.componentInstance;

      expect(c.form.getRawValue()).toEqual(expect.objectContaining(quoteState));
      expect(c.form.controls.productId.disabled).toBe(true);
      expect(c.form.controls.sumAssured.disabled).toBe(true);
      expect(c.profile()).toEqual({ customerId: 'cust1' });
      expect(c.familyMembers()).toEqual([{ id: 'm1' }]);
      expect(c.docRequirements()).toEqual(docRequirements);
      expect(c.docRequirementsLoaded()).toBe(true);
    });

    it('prefills and locks quote-derived motor details', () => {
      withHistoryState(motorQuoteState);
      const fixture = create({ requirements: [] });
      const c = fixture.componentInstance;

      expect(c.quoteMotorDetail()).toEqual(motorQuoteState.motorDetail);
      expect(c.motorForm.getRawValue()).toEqual(expect.objectContaining({
        vehicleMake: 'TVS',
        vehicleModel: 'Jupiter',
        manufactureYear: 2023,
      }));
      expect(c.motorForm.controls.vehicleMake.disabled).toBe(true);
      expect(c.motorForm.controls.vehicleModel.disabled).toBe(true);
      expect(c.motorForm.controls.manufactureYear.disabled).toBe(true);
      expect(c.motorForm.controls.motorVehicleType.disabled).toBe(true);
    });

    it('marks doc requirements loaded and warns when they fail to load', () => {
      withHistoryState(quoteState);
      const fixture = create({ requirementsError: true });
      expect(fixture.componentInstance.docRequirementsLoaded()).toBe(true);
      expect(toast.warning).toHaveBeenCalledWith('Document requirements could not be loaded.');
    });
  });

  describe('nominees', () => {
    it('starts with exactly one nominee group', () => {
      withHistoryState(lifeQuoteState);
      const fixture = create();
      expect(fixture.componentInstance.nominees).toHaveLength(1);
    });

    it('addNominee appends another group', () => {
      withHistoryState(lifeQuoteState);
      const fixture = create();
      fixture.componentInstance.addNominee();
      expect(fixture.componentInstance.nominees).toHaveLength(2);
    });

    it('totalShares sums sharePercentage across all nominee groups', () => {
      withHistoryState(lifeQuoteState);
      const fixture = create();
      const c = fixture.componentInstance;
      c.nominees.at(0).patchValue({ sharePercentage: 60 });
      c.addNominee();
      c.nominees.at(1).patchValue({ sharePercentage: 40 });
      expect(c.totalShares).toBe(100);
    });

    it('isMinorNominee is true for a nominee under 18 as of today', () => {
      withHistoryState(lifeQuoteState);
      const fixture = create();
      fixture.componentInstance.nominees.at(0).patchValue({ dateOfBirth: '2015-01-01' });
      expect(fixture.componentInstance.isMinorNominee(0)).toBe(true);
    });

    it('isMinorNominee is false for a nominee 18 or older', () => {
      withHistoryState(lifeQuoteState);
      const fixture = create();
      fixture.componentInstance.nominees.at(0).patchValue({ dateOfBirth: '1990-01-01' });
      expect(fixture.componentInstance.isMinorNominee(0)).toBe(false);
    });

    describe('nomineesValid', () => {
      function validNominee(fixture: ReturnType<typeof create>) {
        fixture.componentInstance.nominees.at(0).setValue({
          name: 'Jane Doe', relationship: 'Spouse', sharePercentage: 100, dateOfBirth: '1990-01-01', appointeeName: '',
        });
      }

      it('is false when totalShares is not exactly 100', () => {
        withHistoryState(lifeQuoteState);
        const fixture = create();
        validNominee(fixture);
        fixture.componentInstance.nominees.at(0).patchValue({ sharePercentage: 50 });
        expect(fixture.componentInstance.nomineesValid).toBe(false);
      });

      it('is false when there are no nominees', () => {
        withHistoryState(lifeQuoteState);
        const fixture = create();
        fixture.componentInstance.nominees.clear();
        expect(fixture.componentInstance.nomineesValid).toBe(false);
      });

      it('is false when a minor nominee has no appointeeName', () => {
        withHistoryState(lifeQuoteState);
        const fixture = create();
        validNominee(fixture);
        fixture.componentInstance.nominees.at(0).patchValue({ dateOfBirth: '2015-01-01', appointeeName: '' });
        expect(fixture.componentInstance.nomineesValid).toBe(false);
      });

      it('is true when a minor nominee has an appointeeName', () => {
        withHistoryState(lifeQuoteState);
        const fixture = create();
        validNominee(fixture);
        fixture.componentInstance.nominees.at(0).patchValue({ dateOfBirth: '2015-01-01', appointeeName: 'Guardian Name' });
        expect(fixture.componentInstance.nomineesValid).toBe(true);
      });

      it('is true for a complete adult nominee summing to 100%', () => {
        withHistoryState(lifeQuoteState);
        const fixture = create();
        validNominee(fixture);
        expect(fixture.componentInstance.nomineesValid).toBe(true);
      });

      it('is true without nominees for non-Life products', () => {
        withHistoryState(motorQuoteState);
        const fixture = create({ requirements: [] });
        fixture.componentInstance.nominees.clear();
        expect(fixture.componentInstance.nomineesValid).toBe(true);
      });
    });
  });

  describe('onDocSelected / requiredDocumentsUploaded', () => {
    it('is false until all mandatory documents are uploaded', () => {
      withHistoryState(quoteState);
      const fixture = create();
      expect(fixture.componentInstance.requiredDocumentsUploaded()).toBe(false);
    });

    it('is true once every mandatory document is uploaded (optional ones are not required)', () => {
      withHistoryState(quoteState);
      const fixture = create();
      fixture.componentInstance.onDocSelected('ID_PROOF', new File(['x'], 'id.pdf'));
      expect(fixture.componentInstance.requiredDocumentsUploaded()).toBe(true);
    });
  });

  describe('submit', () => {
    function fillValidNominee(fixture: ReturnType<typeof create>) {
      fixture.componentInstance.nominees.at(0).setValue({
        name: 'Jane Doe', relationship: 'Spouse', sharePercentage: 100, dateOfBirth: '1990-01-01', appointeeName: '',
      });
      fixture.componentInstance.onDocSelected('ID_PROOF', new File(['x'], 'id.pdf'));
    }

    it('shows an error and does not submit when the profile has no customerId yet', () => {
      withHistoryState(quoteState);
      const fixture = create({ profile: {} as UserDto });
      fillValidNominee(fixture);

      fixture.componentInstance.submit();

      expect(toast.error).toHaveBeenCalledWith('Customer profile is not ready yet');
      expect(proposalService.submit).not.toHaveBeenCalled();
    });

    it('warns and does not submit when nominees are invalid', () => {
      withHistoryState(lifeQuoteState);
      const fixture = create();
      fixture.componentInstance.onDocSelected('ID_PROOF', new File(['x'], 'id.pdf'));
      // leave nominee invalid (missing required fields)

      fixture.componentInstance.submit();

      expect(toast.warning).toHaveBeenCalledWith('Please complete all required fields before submitting.');
      expect(proposalService.submit).not.toHaveBeenCalled();
    });

    it('warns and does not submit when required documents are missing', () => {
      withHistoryState(quoteState);
      const fixture = create();
      fixture.componentInstance.nominees.at(0).setValue({
        name: 'Jane Doe', relationship: 'Spouse', sharePercentage: 100, dateOfBirth: '1990-01-01', appointeeName: '',
      });

      fixture.componentInstance.submit();

      expect(toast.warning).toHaveBeenCalledWith('Please upload all required documents before submitting.');
      expect(proposalService.submit).not.toHaveBeenCalled();
    });

    it('submits the mapped payload and navigates without uploads when there are no extra files', () => {
      withHistoryState(lifeQuoteState);
      const fixture = create({ requirements: [] });
      fixture.componentInstance.nominees.at(0).setValue({
        name: 'Jane Doe', relationship: 'Spouse', sharePercentage: 100, dateOfBirth: '1990-01-01', appointeeName: '',
      });
      proposalService.submit.mockReturnValue(of({ id: 'proposal1' } as ProposalDto));

      fixture.componentInstance.submit();

      expect(proposalService.submit).toHaveBeenCalledWith(expect.objectContaining({
        customerId: 'cust1',
        productId: 'prod1',
        nominees: [expect.objectContaining({ fullName: 'Jane Doe', sharePercentage: 100, isMinor: false })],
      }));
      expect(toast.success).toHaveBeenCalledWith('Proposal submitted');
      expect(router.navigate).toHaveBeenCalledWith(['/proposals', 'proposal1']);
    });

    it('requires Motor engine and chassis numbers before submitting', () => {
      withHistoryState(motorQuoteState);
      const fixture = create({ requirements: [] });
      fixture.componentInstance.motorForm.patchValue({
        vehicleNumber: 'TN 09 AB 1234',
        engineNumber: '',
        chassisNumber: '',
      });

      fixture.componentInstance.submit();

      expect(toast.warning).toHaveBeenCalledWith('Please complete the vehicle details before submitting.');
      expect(proposalService.submit).not.toHaveBeenCalled();
    });

    it('submits Motor engine and chassis numbers in motorDetail', () => {
      withHistoryState(motorQuoteState);
      const fixture = create({ requirements: [] });
      fixture.componentInstance.motorForm.patchValue({
        vehicleNumber: 'TN 09 AB 1234',
        engineNumber: 'K12MN1234567',
        chassisNumber: 'MA3FHEB1S00A12345',
      });
      proposalService.submit.mockReturnValue(of({ id: 'proposal1' } as ProposalDto));

      fixture.componentInstance.submit();

      expect(proposalService.submit).toHaveBeenCalledWith(expect.objectContaining({
        nominees: [],
        motorDetail: expect.objectContaining({
          vehicleMake: 'TVS',
          vehicleModel: 'Jupiter',
          manufactureYear: 2023,
          vehicleType: 'TwoWheeler',
          idv: 75000,
          engineNumber: 'K12MN1234567',
          chassisNumber: 'MA3FHEB1S00A12345',
        }),
      }));
    });

    it('uploads the required document after submit succeeds', () => {
      withHistoryState(quoteState);
      const fixture = create();
      fillValidNominee(fixture);
      proposalService.submit.mockReturnValue(of({ id: 'proposal1' } as ProposalDto));
      proposalService.uploadDocument.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.submit();

      expect(proposalService.uploadDocument).toHaveBeenCalledWith('proposal1', 'ID_PROOF', expect.any(File));
      expect(toast.success).toHaveBeenCalledWith('Proposal submitted');
    });

    it('warns but still navigates when a document upload fails', () => {
      withHistoryState(quoteState);
      const fixture = create();
      fillValidNominee(fixture);
      proposalService.submit.mockReturnValue(of({ id: 'proposal1' } as ProposalDto));
      proposalService.uploadDocument.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.submit();

      expect(toast.warning).toHaveBeenCalledWith('Proposal submitted but some documents failed');
      expect(router.navigate).toHaveBeenCalledWith(['/proposals', 'proposal1']);
    });

    it('shows an error and resets submitting when the submit call fails', () => {
      withHistoryState(quoteState);
      const fixture = create();
      fillValidNominee(fixture);
      proposalService.submit.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.submit();

      expect(toast.error).toHaveBeenCalledWith('Submission failed');
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('sets submitting true while in flight and blocks a duplicate submit', () => {
      withHistoryState(quoteState);
      const fixture = create({ requirements: [] });
      fixture.componentInstance.nominees.at(0).setValue({
        name: 'Jane Doe', relationship: 'Spouse', sharePercentage: 100, dateOfBirth: '1990-01-01', appointeeName: '',
      });
      const subject = new Subject<ProposalDto>();
      proposalService.submit.mockReturnValue(subject.asObservable());

      fixture.componentInstance.submit();
      expect(fixture.componentInstance.submitting()).toBe(true);

      fixture.componentInstance.submit();
      expect(proposalService.submit).toHaveBeenCalledTimes(1);

      subject.next({ id: 'proposal1' } as ProposalDto);
      subject.complete();
      expect(toast.success).toHaveBeenCalledWith('Proposal submitted');
    });
  });

  describe('canDeactivate', () => {
    it('allows navigation when nothing has been touched', () => {
      withHistoryState(quoteState);
      const fixture = create();
      expect(fixture.componentInstance.canDeactivate()).toBe(true);
    });

    it('prompts for confirmation when a nominee field has been edited', () => {
      withHistoryState(quoteState);
      const fixture = create();
      fixture.componentInstance.nominees.markAsDirty();

      const result = fixture.componentInstance.canDeactivate();

      expect(fixture.componentInstance.showLeaveConfirm()).toBe(true);
      expect(result).not.toBe(true);
    });

    it('prompts for confirmation when a document has been attached', () => {
      withHistoryState(quoteState);
      const fixture = create();
      fixture.componentInstance.onDocSelected('ID_PROOF', new File(['x'], 'id.pdf'));

      expect(fixture.componentInstance.canDeactivate()).not.toBe(true);
    });

    it('resolves true on confirmLeave and false on cancelLeave', async () => {
      withHistoryState(quoteState);
      const fixture = create();
      fixture.componentInstance.nominees.markAsDirty();

      const result$ = fixture.componentInstance.canDeactivate();
      const resultPromise = new Promise(resolve => (result$ as any).subscribe(resolve));
      fixture.componentInstance.confirmLeave();

      expect(await resultPromise).toBe(true);
      expect(fixture.componentInstance.showLeaveConfirm()).toBe(false);
    });

    it('allows navigation without prompting right after a successful submit', () => {
      withHistoryState(quoteState);
      const fixture = create({ requirements: [] });
      fixture.componentInstance.nominees.at(0).setValue({
        name: 'Jane Doe', relationship: 'Spouse', sharePercentage: 100, dateOfBirth: '1990-01-01', appointeeName: '',
      });
      proposalService.submit.mockReturnValue(of({ id: 'proposal1' } as ProposalDto));

      fixture.componentInstance.submit();

      expect(fixture.componentInstance.canDeactivate()).toBe(true);
    });
  });
});
