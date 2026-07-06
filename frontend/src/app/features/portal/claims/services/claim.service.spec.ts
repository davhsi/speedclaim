import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ClaimService } from './claim.service';
import { IntimateClaimRequest } from '../../../../core/models/api.models';

describe('ClaimService', () => {
  let service: ClaimService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ClaimService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('getMyClaims', () => {
    it('GETs with no params when status/type are omitted', () => {
      let result: unknown;
      service.getMyClaims().subscribe(res => (result = res));
      const call = httpMock.expectOne(r => r.url === '/api/v1/claims/my');
      expect(call.request.method).toBe('GET');
      expect(call.request.params.has('status')).toBe(false);
      expect(call.request.params.has('type')).toBe(false);
      call.flush([{ id: 'c1' }]);
      expect(result).toEqual([{ id: 'c1' }]);
    });

    it('includes status and type params when provided', () => {
      service.getMyClaims('Approved', 'Motor').subscribe();
      const call = httpMock.expectOne(r => r.url === '/api/v1/claims/my');
      expect(call.request.params.get('status')).toBe('Approved');
      expect(call.request.params.get('type')).toBe('Motor');
      call.flush([]);
    });
  });

  it('getById GETs the claim by id', () => {
    let result: unknown;
    service.getById('claim-1').subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/claims/claim-1');
    expect(call.request.method).toBe('GET');
    call.flush({ id: 'claim-1' });
    expect(result).toEqual({ id: 'claim-1' });
  });

  it('getHistory GETs the claim status history', () => {
    let result: unknown;
    service.getHistory('claim-1').subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/claims/claim-1/history');
    expect(call.request.method).toBe('GET');
    call.flush([{ status: 'Intimated' }]);
    expect(result).toEqual([{ status: 'Intimated' }]);
  });

  it('intimate POSTs the intimation request', () => {
    const req: IntimateClaimRequest = {
      policyId: 'pol-1',
      claimType: 'Motor' as IntimateClaimRequest['claimType'],
      claimAmountRequested: 5000,
      incidentDate: '2026-01-01',
      incidentDescription: 'Fender bender',
      isCashless: false,
    };
    let result: unknown;
    service.intimate(req).subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/claims/intimate');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush({ id: 'claim-new' });
    expect(result).toEqual({ id: 'claim-new' });
  });

  it('uploadDocument PUTs a FormData body containing the file', () => {
    const file = new File(['data'], 'doc.pdf', { type: 'application/pdf' });
    let result: unknown;
    service.uploadDocument('claim-1', 'policeReport', file).subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/claims/claim-1/documents/policeReport');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toBeInstanceOf(FormData);
    expect((call.request.body as FormData).get('file')).toBe(file);
    call.flush({ message: 'uploaded' });
    expect(result).toEqual({ message: 'uploaded' });
  });

  it('withdraw PUTs to the withdraw endpoint', () => {
    let called = false;
    service.withdraw('claim-1').subscribe(() => (called = true));
    const call = httpMock.expectOne('/api/v1/claims/claim-1/withdraw');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({});
    call.flush(null);
    expect(called).toBe(true);
  });
});
