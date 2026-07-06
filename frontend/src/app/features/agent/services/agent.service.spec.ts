import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AgentService } from './agent.service';

describe('AgentService', () => {
  let service: AgentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AgentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getDashboard GETs the agent dashboard summary', () => {
    const response = { totalCustomers: 5, totalPolicies: 10, totalCommission: 100, pendingClaims: 2 };
    service.getDashboard().subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/agents/dashboard');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('getCustomers GETs the assigned customer list', () => {
    const response = [{ id: 'c1' }];
    service.getCustomers().subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/agents/customers');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('getProfile GETs the agent profile', () => {
    const response = { agentId: 'a1' };
    service.getProfile().subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/agents/profile');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('updateProfile PATCHes the profile payload', () => {
    const req = { salutation: 'Mr', firstName: 'Jane', lastName: 'Doe', phone: '9876543210' };
    const response = { message: 'ok' };
    service.updateProfile(req).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/agents/profile');
    expect(call.request.method).toBe('PATCH');
    expect(call.request.body).toEqual(req);
    call.flush(response);
  });

  it('getRenewals GETs the renewal reminders list', () => {
    const response = [{ policyId: 'p1' }];
    service.getRenewals().subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/agents/renewals');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('getAssignedPolicies GETs the assigned policy list', () => {
    const response = [{ id: 'pol1' }];
    service.getAssignedPolicies().subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/policies/assigned');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('getMyProposals GETs the agent\'s proposal list', () => {
    const response = [{ id: 'prop1' }];
    service.getMyProposals().subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/proposals/my');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('getProposalById GETs a single proposal by id', () => {
    const response = { id: 'prop1' };
    service.getProposalById('prop1').subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/proposals/prop1');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('submitProposal POSTs the proposal payload', () => {
    const req = { productId: 'p1' } as never;
    const response = { id: 'prop1' };
    service.submitProposal(req).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/proposals');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush(response);
  });

  it('getProducts GETs the products list', () => {
    const response = [{ id: 'p1' }];
    service.getProducts().subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/products');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('generateQuote POSTs the quote request', () => {
    const req = { productId: 'p1' } as never;
    const response = { premiumAmount: 5000 };
    service.generateQuote(req).subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/proposals/quote');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    call.flush(response);
  });

  it('getCommissions GETs the commission list', () => {
    const response = [{ id: 'com1' }];
    service.getCommissions().subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/agents/commissions');
    expect(call.request.method).toBe('GET');
    call.flush(response);
  });

  it('withdrawProposal PUTs an empty body to the withdraw endpoint', () => {
    const response = { message: 'ok' };
    service.withdrawProposal('prop1').subscribe(res => expect(res).toEqual(response));
    const call = httpMock.expectOne('/api/v1/proposals/prop1/withdraw');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({});
    call.flush(response);
  });
});
