import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PolicyService } from './policy.service';
import {
  ApiMessage, EndorsementDto, PolicyDto, PolicyNomineeDto, PolicyStatusHistoryDto,
  PremiumScheduleDto, RequestEndorsementRequest, UpdateNomineeRequest,
} from '../../../../core/models/api.models';

describe('PolicyService', () => {
  let service: PolicyService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PolicyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getMyPolicies performs a GET to /api/v1/policies/my without a status param when omitted', () => {
    const policies = [{ id: 'pol1' }] as PolicyDto[];
    let result: PolicyDto[] | undefined;

    service.getMyPolicies().subscribe(res => (result = res));

    const call = httpMock.expectOne(req => req.url === '/api/v1/policies/my');
    expect(call.request.method).toBe('GET');
    expect(call.request.params.has('status')).toBe(false);
    call.flush(policies);

    expect(result).toEqual(policies);
  });

  it('getMyPolicies includes the status param when provided', () => {
    const policies = [{ id: 'pol1' }] as PolicyDto[];

    service.getMyPolicies('Active').subscribe();

    const call = httpMock.expectOne(req => req.url === '/api/v1/policies/my');
    expect(call.request.params.get('status')).toBe('Active');
    call.flush(policies);
  });

  it('getById performs a GET to /api/v1/policies/:id', () => {
    const policy = { id: 'pol1' } as PolicyDto;
    let result: PolicyDto | undefined;

    service.getById('pol1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/policies/pol1');
    expect(call.request.method).toBe('GET');
    call.flush(policy);

    expect(result).toEqual(policy);
  });

  it('getHistory performs a GET to /api/v1/policies/:id/history', () => {
    const history = [{ id: 'h1' }] as PolicyStatusHistoryDto[];
    let result: PolicyStatusHistoryDto[] | undefined;

    service.getHistory('pol1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/policies/pol1/history');
    expect(call.request.method).toBe('GET');
    call.flush(history);

    expect(result).toEqual(history);
  });

  it('getEndorsements performs a GET to /api/v1/policies/:id/endorsements', () => {
    const endorsements = [{ id: 'e1' }] as EndorsementDto[];
    let result: EndorsementDto[] | undefined;

    service.getEndorsements('pol1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/policies/pol1/endorsements');
    expect(call.request.method).toBe('GET');
    call.flush(endorsements);

    expect(result).toEqual(endorsements);
  });

  it('requestEndorsement performs a POST to /api/v1/policies/:id/endorsements with the request body', () => {
    const req = { type: 'AddressChange' } as unknown as RequestEndorsementRequest;
    const response = { message: 'ok' } as ApiMessage;
    let result: ApiMessage | undefined;

    service.requestEndorsement('pol1', req).subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/policies/pol1/endorsements');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush(response);

    expect(result).toEqual(response);
  });

  it('getNominees performs a GET to /api/v1/policies/:id/nominees', () => {
    const nominees = [{ id: 'n1' }] as PolicyNomineeDto[];
    let result: PolicyNomineeDto[] | undefined;

    service.getNominees('pol1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/policies/pol1/nominees');
    expect(call.request.method).toBe('GET');
    call.flush(nominees);

    expect(result).toEqual(nominees);
  });

  it('updateNominee performs a PATCH to /api/v1/policies/nominees/:nomineeId with the request body', () => {
    const req = { name: 'Jane' } as unknown as UpdateNomineeRequest;
    const response = { message: 'ok' } as ApiMessage;
    let result: ApiMessage | undefined;

    service.updateNominee('nom1', req).subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/policies/nominees/nom1');
    expect(call.request.method).toBe('PATCH');
    expect(call.request.body).toEqual(req);
    call.flush(response);

    expect(result).toEqual(response);
  });

  it('cancelPolicy performs a PUT to /api/v1/policies/:id/cancel', () => {
    const response = { message: 'cancelled' } as ApiMessage;
    let result: ApiMessage | undefined;

    service.cancelPolicy('pol1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/policies/pol1/cancel');
    expect(call.request.method).toBe('PUT');
    call.flush(response);

    expect(result).toEqual(response);
  });

  it('getSchedule performs a GET to /api/v1/payments/schedule/:id', () => {
    const schedule = [{ id: 'ps1' }] as PremiumScheduleDto[];
    let result: PremiumScheduleDto[] | undefined;

    service.getSchedule('pol1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/payments/schedule/pol1');
    expect(call.request.method).toBe('GET');
    call.flush(schedule);

    expect(result).toEqual(schedule);
  });

  it('downloadCertificate performs a GET to /api/v1/policies/:id/download with a blob response type', () => {
    const blob = new Blob(['pdf-bytes'], { type: 'application/pdf' });
    let result: Blob | undefined;

    service.downloadCertificate('pol1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/policies/pol1/download');
    expect(call.request.method).toBe('GET');
    expect(call.request.responseType).toBe('blob');
    call.flush(blob);

    expect(result).toEqual(blob);
  });
});
