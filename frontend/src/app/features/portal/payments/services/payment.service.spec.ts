import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PaymentService } from './payment.service';
import {
  CreatePaymentIntentRequest, CreatePaymentIntentResponse,
  PaymentRecordDto, PremiumScheduleDto, SavedCardDto,
} from '../../../../core/models/api.models';

describe('PaymentService', () => {
  let service: PaymentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PaymentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getSchedule performs a GET to /api/v1/payments/schedule/:policyId', () => {
    const schedule = [{ id: 'ps1' }] as PremiumScheduleDto[];
    let result: PremiumScheduleDto[] | undefined;

    service.getSchedule('policy1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/payments/schedule/policy1');
    expect(call.request.method).toBe('GET');
    call.flush(schedule);

    expect(result).toEqual(schedule);
  });

  it('createPaymentIntent performs a POST to /api/v1/payments/pay/:scheduleId with the body and an Idempotency-Key header', () => {
    const req: CreatePaymentIntentRequest = { policyId: 'policy1' };
    const response = { clientSecret: 'secret', publishableKey: 'pk_test' } as CreatePaymentIntentResponse;
    let result: CreatePaymentIntentResponse | undefined;

    service.createPaymentIntent('schedule1', req).subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/payments/pay/schedule1');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(req);
    expect(call.request.headers.has('Idempotency-Key')).toBe(true);
    call.flush(response);

    expect(result).toEqual(response);
  });

  it('createPaymentIntent sends a different Idempotency-Key on each call', () => {
    const req: CreatePaymentIntentRequest = { policyId: 'policy1' };

    service.createPaymentIntent('schedule1', req).subscribe();
    const firstCall = httpMock.expectOne('/api/v1/payments/pay/schedule1');
    const firstKey = firstCall.request.headers.get('Idempotency-Key');
    firstCall.flush({});

    service.createPaymentIntent('schedule1', req).subscribe();
    const secondCall = httpMock.expectOne('/api/v1/payments/pay/schedule1');
    const secondKey = secondCall.request.headers.get('Idempotency-Key');
    secondCall.flush({});

    expect(firstKey).not.toBe(secondKey);
  });

  it('getHistory performs a GET to /api/v1/payments/history', () => {
    const history = [{ id: 'p1' }] as PaymentRecordDto[];
    let result: PaymentRecordDto[] | undefined;

    service.getHistory().subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/payments/history');
    expect(call.request.method).toBe('GET');
    call.flush(history);

    expect(result).toEqual(history);
  });

  it('getReceipt performs a GET to /api/v1/payments/:paymentId/receipt', () => {
    const receipt = { id: 'p1' } as PaymentRecordDto;
    let result: PaymentRecordDto | undefined;

    service.getReceipt('p1').subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/payments/p1/receipt');
    expect(call.request.method).toBe('GET');
    call.flush(receipt);

    expect(result).toEqual(receipt);
  });

  it('getMethods performs a GET to /api/v1/payments/methods', () => {
    const methods = [{ id: 'card1' }] as SavedCardDto[];
    let result: SavedCardDto[] | undefined;

    service.getMethods().subscribe(res => (result = res));

    const call = httpMock.expectOne('/api/v1/payments/methods');
    expect(call.request.method).toBe('GET');
    call.flush(methods);

    expect(result).toEqual(methods);
  });
});
