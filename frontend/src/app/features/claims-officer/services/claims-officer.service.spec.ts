import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ClaimsOfficerService } from './claims-officer.service';

describe('ClaimsOfficerService', () => {
  let service: ClaimsOfficerService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ClaimsOfficerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getAllClaims sends page/pageSize and includes status/type filters when provided', () => {
    const response = { items: [], total: 0 };
    service.getAllClaims(2, 10, 'UnderReview' as never, 'Motor' as never).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne(r => r.url === '/api/v1/claims/all');
    expect(call.request.method).toBe('GET');
    expect(call.request.params.get('page')).toBe('2');
    expect(call.request.params.get('pageSize')).toBe('10');
    expect(call.request.params.get('status')).toBe('UnderReview');
    expect(call.request.params.get('type')).toBe('Motor');
    call.flush(response);
  });

  it('getAllClaims omits status/type params when not provided', () => {
    service.getAllClaims().subscribe();
    const call = httpMock.expectOne(r => r.url === '/api/v1/claims/all');
    expect(call.request.params.get('page')).toBe('1');
    expect(call.request.params.get('pageSize')).toBe('20');
    expect(call.request.params.has('status')).toBe(false);
    expect(call.request.params.has('type')).toBe(false);
    call.flush({});
  });

  it('getClaimById GETs a single claim by id', () => {
    const response = { id: 'c1' };
    service.getClaimById('c1').subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/claims/c1');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('getClaimHistory GETs the status history for a claim', () => {
    const response = [{ status: 'Intimated' }];
    service.getClaimHistory('c1').subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/claims/c1/history');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('assignToSelf PUTs an empty body to the assign endpoint', () => {
    const response = { message: 'ok' };
    service.assignToSelf('c1').subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/claims/c1/assign');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({});
    call.flush(response);
  });

  it('approveReject PUTs the approve/reject request body', () => {
    const req = { isApproved: true, approvedAmount: 5000, reason: 'valid claim' };
    const response = { message: 'ok' };
    service.approveReject('c1', req).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/claims/c1/approve');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual(req);
    call.flush(response);
  });


  it('updateStatus PUTs the status update request body', () => {
    const req = { status: 'DocumentsPending' as never, remarks: 'need docs' };
    const response = { message: 'ok' };
    service.updateStatus('c1', req).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/claims/c1/status');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual(req);
    call.flush(response);
  });

  it('assignSurveyor PUTs the surveyor assignment request body', () => {
    const req = { surveyorId: 's1', notes: 'urgent' };
    const response = { message: 'ok' };
    service.assignSurveyor('c1', req).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/claims/c1/assign-surveyor');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual(req);
    call.flush(response);
  });

  it('requestDocs POSTs the details as a JSON string body with an explicit content-type', () => {
    const response = { message: 'ok' };
    service.requestDocs('c1', 'need the RC copy').subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/claims/c1/request-docs');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toBe(JSON.stringify('need the RC copy'));
    expect(call.request.headers.get('Content-Type')).toBe('application/json');
    call.flush(response);
  });

  it('approvePreAuth PUTs an empty body to the approve-preauth endpoint', () => {
    const response = { message: 'ok' };
    service.approvePreAuth('c1').subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/claims/c1/approve-preauth');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({});
    call.flush(response);
  });

  it('getAllGrievances sends page/pageSize params', () => {
    const response = { items: [], total: 0 };
    service.getAllGrievances(3, 15).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne(r => r.url === '/api/v1/grievances/all');
    expect(call.request.method).toBe('GET');
    expect(call.request.params.get('page')).toBe('3');
    expect(call.request.params.get('pageSize')).toBe('15');
    call.flush(response);
  });

  it('getGrievanceById GETs a single grievance by id', () => {
    const response = { id: 'g1' };
    service.getGrievanceById('g1').subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/grievances/g1');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('assignGrievance PUTs the assignment request body', () => {
    const req = { assignedToId: 'officer1' };
    const response = { message: 'ok' };
    service.assignGrievance('g1', req).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/grievances/g1/assign');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual(req);
    call.flush(response);
  });

  it('updateGrievanceStatus PUTs the status update request body', () => {
    const req = { status: 'Resolved' as never, resolutionNotes: 'fixed' };
    const response = { message: 'ok' };
    service.updateGrievanceStatus('g1', req).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/grievances/g1/status');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual(req);
    call.flush(response);
  });

  it('getSurveyors GETs the surveyor list', () => {
    const response = [{ id: 'sv1' }];
    service.getSurveyors().subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/users/surveyors');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });
});
