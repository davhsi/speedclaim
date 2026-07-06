import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  let service: DashboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getPolicies GETs the customer\'s policies', () => {
    let result: unknown;
    service.getPolicies().subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/policies/my');
    expect(call.request.method).toBe('GET');
    call.flush([{ id: 'pol1' }]);
    expect(result).toEqual([{ id: 'pol1' }]);
  });

  it('getClaims GETs the customer\'s claims', () => {
    let result: unknown;
    service.getClaims().subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/claims/my');
    expect(call.request.method).toBe('GET');
    call.flush([{ id: 'claim1' }]);
    expect(result).toEqual([{ id: 'claim1' }]);
  });

  it('getSchedule GETs the premium schedule for a policy', () => {
    let result: unknown;
    service.getSchedule('pol1').subscribe(res => (result = res));
    const call = httpMock.expectOne('/api/v1/payments/schedule/pol1');
    expect(call.request.method).toBe('GET');
    call.flush([{ installmentNumber: 1 }]);
    expect(result).toEqual([{ installmentNumber: 1 }]);
  });
});
