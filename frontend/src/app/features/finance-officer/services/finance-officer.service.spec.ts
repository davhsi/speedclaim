import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { FinanceOfficerService } from './finance-officer.service';

describe('FinanceOfficerService', () => {
  let service: FinanceOfficerService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FinanceOfficerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getAllPaymentRecords GETs all payment records', () => {
    let result: unknown;
    service.getAllPaymentRecords().subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/payments/all-records');
    expect(call.request.method).toBe('GET');
    call.flush([{ id: 'p1' }]);
    expect(result).toEqual([{ id: 'p1' }]);
  });

  it('reconcilePayment PUTs to the reconcile endpoint', () => {
    let result: unknown;
    service.reconcilePayment('pay-1').subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/payments/pay-1/reconcile');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({});
    call.flush({ message: 'ok' });
    expect(result).toEqual({ message: 'ok' });
  });

  it('refundPayment POSTs with an Idempotency-Key header', () => {
    let result: unknown;
    service.refundPayment('pay-1').subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/payments/pay-1/refund');
    expect(call.request.method).toBe('POST');
    expect(call.request.headers.has('Idempotency-Key')).toBe(true);
    call.flush({ message: 'refunded' });
    expect(result).toEqual({ message: 'refunded' });
  });

  it('getClaimsForPayout GETs with fixed page/pageSize and no status by default', () => {
    let result: unknown;
    service.getClaimsForPayout().subscribe(res => (result = res));
    const call = httpMock.expectOne(r => r.url === '/api/v1/claims/all');
    expect(call.request.method).toBe('GET');
    expect(call.request.params.get('page')).toBe('1');
    expect(call.request.params.get('pageSize')).toBe('100');
    expect(call.request.params.has('status')).toBe(false);
    call.flush({ items: [], totalCount: 0 });
    expect(result).toEqual({ items: [], totalCount: 0 });
  });

  it('getClaimsForPayout includes status when provided', () => {
    service.getClaimsForPayout('Approved' as never).subscribe();
    const call = httpMock.expectOne(r => r.url === '/api/v1/claims/all');
    expect(call.request.params.get('status')).toBe('Approved');
    call.flush({ items: [], totalCount: 0 });
  });

  it('processClaimPayout POSTs with an Idempotency-Key header', () => {
    let result: unknown;
    service.processClaimPayout('claim-1').subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/payments/payout/claim/claim-1');
    expect(call.request.method).toBe('POST');
    expect(call.request.headers.has('Idempotency-Key')).toBe(true);
    call.flush({ message: 'paid out' });
    expect(result).toEqual({ message: 'paid out' });
  });

  it('markClaimSettled PUTs to the settle endpoint', () => {
    let result: unknown;
    service.markClaimSettled('claim-1').subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/payments/claims/claim-1/settle');
    expect(call.request.method).toBe('PUT');
    call.flush({ message: 'settled' });
    expect(result).toEqual({ message: 'settled' });
  });

  it('getPendingCommissions GETs pending commissions', () => {
    let result: unknown;
    service.getPendingCommissions().subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/payments/commissions/pending');
    expect(call.request.method).toBe('GET');
    call.flush([{ id: 'c1' }]);
    expect(result).toEqual([{ id: 'c1' }]);
  });

  it('approveCommission POSTs with an Idempotency-Key header', () => {
    let result: unknown;
    service.approveCommission('c1').subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/payments/commissions/c1/approve');
    expect(call.request.method).toBe('POST');
    expect(call.request.headers.has('Idempotency-Key')).toBe(true);
    call.flush({ message: 'approved' });
    expect(result).toEqual({ message: 'approved' });
  });

  it('getOverduePolicies GETs overdue policies', () => {
    let result: unknown;
    service.getOverduePolicies().subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/payments/reports/overdue');
    expect(call.request.method).toBe('GET');
    call.flush([{ policyId: 'pol1' }]);
    expect(result).toEqual([{ policyId: 'pol1' }]);
  });

  it('getCollectionSummary GETs the summary with the period param', () => {
    let result: unknown;
    service.getCollectionSummary('monthly').subscribe(res => (result = res));
    const call = httpMock.expectOne(r => r.url === '/api/v1/payments/reports/summary');
    expect(call.request.method).toBe('GET');
    expect(call.request.params.get('period')).toBe('monthly');
    call.flush({ totalCollected: 100 });
    expect(result).toEqual({ totalCollected: 100 });
  });

  describe('exportPaymentReport', () => {
    it('requests a blob with no from/to params when omitted', () => {
      let result: unknown;
      service.exportPaymentReport().subscribe(res => (result = res));
      const call = httpMock.expectOne(r => r.url === '/api/v1/payments/reports/export');
      expect(call.request.method).toBe('GET');
      expect(call.request.responseType).toBe('blob');
      expect(call.request.params.has('from')).toBe(false);
      expect(call.request.params.has('to')).toBe(false);
      const blob = new Blob(['data']);
      call.flush(blob);
      expect(result).toBe(blob);
    });

    it('includes from/to params when provided', () => {
      service.exportPaymentReport('2026-01-01', '2026-02-01').subscribe();
      const call = httpMock.expectOne(r => r.url === '/api/v1/payments/reports/export');
      expect(call.request.params.get('from')).toBe('2026-01-01');
      expect(call.request.params.get('to')).toBe('2026-02-01');
      call.flush(new Blob(['data']));
    });
  });

  it('updateProfile PATCHes the profile payload', () => {
    const payload = { firstName: 'Jane', lastName: 'Doe', phone: '9876543210' };
    let result: unknown;
    service.updateProfile(payload).subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/users/profile');
    expect(call.request.method).toBe('PATCH');
    expect(call.request.body).toEqual(payload);
    call.flush({ message: 'updated' });
    expect(result).toEqual({ message: 'updated' });
  });
});
