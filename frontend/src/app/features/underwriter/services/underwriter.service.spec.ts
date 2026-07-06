import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import {
  UnderwriterService, UnderwriterKycDto, ReviewProposalRequest, ApproveRejectEndorsementRequest,
} from './underwriter.service';
import { ProposalDto, PolicyDto, PolicyStatusHistoryDto, EndorsementDto, PagedResponse, ApiMessage } from '../../../core/models/api.models';

describe('UnderwriterService', () => {
  let service: UnderwriterService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(UnderwriterService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('Proposals', () => {
    it('getAllProposals fetches all proposals', () => {
      const proposals = [{ id: 'p1' }] as unknown as ProposalDto[];
      let result: ProposalDto[] | undefined;

      service.getAllProposals().subscribe(r => (result = r));

      const call = httpMock.expectOne('/api/v1/proposals/all');
      expect(call.request.method).toBe('GET');
      call.flush(proposals);
      expect(result).toEqual(proposals);
    });

    it('getProposalById fetches a single proposal', () => {
      const proposal = { id: 'p1' } as unknown as ProposalDto;
      let result: ProposalDto | undefined;

      service.getProposalById('p1').subscribe(r => (result = r));

      const call = httpMock.expectOne('/api/v1/proposals/p1');
      expect(call.request.method).toBe('GET');
      call.flush(proposal);
      expect(result).toEqual(proposal);
    });

    it('reviewProposal posts the approve/reject decision', () => {
      const request: ReviewProposalRequest = { isApproved: true, notes: 'Looks good' };
      const res = { message: 'ok' } as ApiMessage;

      service.reviewProposal('p1', request).subscribe();

      const call = httpMock.expectOne('/api/v1/proposals/p1/review');
      expect(call.request.method).toBe('POST');
      expect(call.request.body).toEqual(request);
      call.flush(res);
    });

    it('requestDocs posts the requested details', () => {
      service.requestDocs('p1', 'Need income proof').subscribe();

      const call = httpMock.expectOne('/api/v1/proposals/p1/request-docs');
      expect(call.request.method).toBe('POST');
      expect(call.request.body).toEqual({ details: 'Need income proof' });
      call.flush({ message: 'ok' });
    });

    it('updateNotes patches the notes field', () => {
      service.updateNotes('p1', 'Reviewed').subscribe();

      const call = httpMock.expectOne('/api/v1/proposals/p1/notes');
      expect(call.request.method).toBe('PATCH');
      expect(call.request.body).toEqual({ notes: 'Reviewed' });
      call.flush({ message: 'ok' });
    });
  });

  describe('KYC', () => {
    it('getPendingKyc requests the page/pageSize params (defaults)', () => {
      const paged = { items: [], total: 0 } as unknown as PagedResponse<UnderwriterKycDto>;
      let result: PagedResponse<UnderwriterKycDto> | undefined;

      service.getPendingKyc().subscribe(r => (result = r));

      const call = httpMock.expectOne(req => req.url === '/api/v1/users/kyc/pending');
      expect(call.request.method).toBe('GET');
      expect(call.request.params.get('page')).toBe('1');
      expect(call.request.params.get('pageSize')).toBe('10');
      call.flush(paged);
      expect(result).toEqual(paged);
    });

    it('getPendingKyc forwards custom page/pageSize', () => {
      service.getPendingKyc(3, 25).subscribe();
      const call = httpMock.expectOne(req => req.url === '/api/v1/users/kyc/pending');
      expect(call.request.params.get('page')).toBe('3');
      expect(call.request.params.get('pageSize')).toBe('25');
      call.flush({ items: [], total: 0 });
    });

    it('getKycByUserId fetches a customer KYC record', () => {
      const kyc = { id: 'k1' } as unknown as UnderwriterKycDto;
      let result: UnderwriterKycDto | undefined;

      service.getKycByUserId('cust-1').subscribe(r => (result = r));

      const call = httpMock.expectOne('/api/v1/users/cust-1/kyc');
      expect(call.request.method).toBe('GET');
      call.flush(kyc);
      expect(result).toEqual(kyc);
    });

    it('reviewKyc sends isApproved/reason as query params with an empty body', () => {
      const kyc = { id: 'k1', kycStatus: 'Approved' } as unknown as UnderwriterKycDto;
      let result: UnderwriterKycDto | undefined;

      service.reviewKyc('cust-1', true, 'All documents verified').subscribe(r => (result = r));

      const call = httpMock.expectOne(req => req.url === '/api/v1/users/cust-1/kyc/review');
      expect(call.request.method).toBe('PUT');
      expect(call.request.params.get('isApproved')).toBe('true');
      expect(call.request.params.get('reason')).toBe('All documents verified');
      expect(call.request.body).toEqual({});
      call.flush(kyc);
      expect(result).toEqual(kyc);
    });
  });

  describe('Endorsements', () => {
    it('getPendingEndorsements requests page/pageSize params', () => {
      const paged = { items: [], total: 0 } as unknown as PagedResponse<EndorsementDto>;

      service.getPendingEndorsements(2, 15).subscribe();

      const call = httpMock.expectOne(req => req.url === '/api/v1/policies/endorsements/pending');
      expect(call.request.method).toBe('GET');
      expect(call.request.params.get('page')).toBe('2');
      expect(call.request.params.get('pageSize')).toBe('15');
      call.flush(paged);
    });

    it('reviewEndorsement puts the approve/reject decision', () => {
      const request: ApproveRejectEndorsementRequest = { isApproved: false, reason: 'Incomplete docs' };

      service.reviewEndorsement('end-1', request).subscribe();

      const call = httpMock.expectOne('/api/v1/policies/endorsements/end-1/review');
      expect(call.request.method).toBe('PUT');
      expect(call.request.body).toEqual(request);
      call.flush({ message: 'ok' });
    });
  });

  describe('Policies', () => {
    it('getAllPolicies requests page/pageSize params', () => {
      const paged = { items: [], total: 0 } as unknown as PagedResponse<PolicyDto>;

      service.getAllPolicies(1, 50).subscribe();

      const call = httpMock.expectOne(req => req.url === '/api/v1/policies/all');
      expect(call.request.method).toBe('GET');
      expect(call.request.params.get('page')).toBe('1');
      expect(call.request.params.get('pageSize')).toBe('50');
      call.flush(paged);
    });

    it('getPolicyById fetches a single policy', () => {
      const policy = { id: 'pol-1' } as unknown as PolicyDto;
      let result: PolicyDto | undefined;

      service.getPolicyById('pol-1').subscribe(r => (result = r));

      const call = httpMock.expectOne('/api/v1/policies/pol-1');
      expect(call.request.method).toBe('GET');
      call.flush(policy);
      expect(result).toEqual(policy);
    });

    it('getPolicyHistory fetches the status history', () => {
      const history = [{ id: 'h1' }] as unknown as PolicyStatusHistoryDto[];
      let result: PolicyStatusHistoryDto[] | undefined;

      service.getPolicyHistory('pol-1').subscribe(r => (result = r));

      const call = httpMock.expectOne('/api/v1/policies/pol-1/history');
      expect(call.request.method).toBe('GET');
      call.flush(history);
      expect(result).toEqual(history);
    });
  });

  describe('Profile', () => {
    it('updateProfile patches the profile fields', () => {
      const payload = { firstName: 'Jane', lastName: 'Doe', phone: '9876543210' };

      service.updateProfile(payload).subscribe();

      const call = httpMock.expectOne('/api/v1/users/profile');
      expect(call.request.method).toBe('PATCH');
      expect(call.request.body).toEqual(payload);
      call.flush({ message: 'ok' });
    });

    it('requestPasswordReset posts the email', () => {
      service.requestPasswordReset('jane@example.com').subscribe();

      const call = httpMock.expectOne('/api/v1/auth/forgot-password');
      expect(call.request.method).toBe('POST');
      expect(call.request.body).toEqual({ email: 'jane@example.com' });
      call.flush({ message: 'ok' });
    });
  });
});
