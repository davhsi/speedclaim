import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AgentCustomerKycComponent } from './customer-kyc';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { KycRecordDto } from '../../../core/models/api.models';

describe('AgentCustomerKycComponent', () => {
  let agentService: { getCustomers: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let httpMock: HttpTestingController;

  const customers: AgentCustomerDto[] = [{ id: 'c1', fullName: 'Jane Doe' } as AgentCustomerDto];

  function create() {
    agentService = { getCustomers: vi.fn(() => of(customers)) };
    toast = { success: vi.fn(), error: vi.fn() };
    router = { navigate: vi.fn(() => Promise.resolve(true)) };

    TestBed.configureTestingModule({
      imports: [AgentCustomerKycComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AgentService, useValue: agentService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {} },
      ],
    });
    const fixture = TestBed.createComponent(AgentCustomerKycComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => httpMock?.verify());

  it('loads customers on init', () => {
    const fixture = create();
    expect(fixture.componentInstance.customers()).toEqual(customers);
  });

  describe('onCustomerChange', () => {
    function changeEvent(value: string): Event {
      const select = document.createElement('select');
      Object.defineProperty(select, 'value', { value });
      return { target: select } as unknown as Event;
    }

    it('clears selection and existing KYC, and skips the fetch when the value is empty', () => {
      const fixture = create();
      fixture.componentInstance.onCustomerChange(changeEvent(''));
      expect(fixture.componentInstance.selectedCustomerId()).toBeNull();
      httpMock.expectNone(() => true);
    });

    it('fetches and stores the existing KYC record for the selected customer', () => {
      const fixture = create();
      const kyc = { id: 'k1' } as KycRecordDto;
      fixture.componentInstance.onCustomerChange(changeEvent('c1'));

      const call = httpMock.expectOne('/api/v1/agents/customers/c1/kyc');
      expect(call.request.method).toBe('GET');
      call.flush(kyc);

      expect(fixture.componentInstance.selectedCustomerId()).toBe('c1');
      expect(fixture.componentInstance.existingKyc()).toEqual(kyc);
    });

    it('swallows a KYC fetch error without throwing', () => {
      const fixture = create();
      fixture.componentInstance.onCustomerChange(changeEvent('c1'));
      const call = httpMock.expectOne('/api/v1/agents/customers/c1/kyc');
      call.flush({ message: 'not found' }, { status: 404, statusText: 'Not Found' });
      expect(fixture.componentInstance.existingKyc()).toBeNull();
    });
  });

  describe('idValid / idError', () => {
    it('validates a 12-digit Aadhaar number', () => {
      const fixture = create();
      fixture.componentInstance.idType.set('aadhaar');
      fixture.componentInstance.idNumber.set('123456789012');
      expect(fixture.componentInstance.idValid()).toBe(true);
      expect(fixture.componentInstance.idError()).toBe('');
    });

    it('flags an invalid Aadhaar number', () => {
      const fixture = create();
      fixture.componentInstance.idType.set('aadhaar');
      fixture.componentInstance.idNumber.set('123');
      expect(fixture.componentInstance.idValid()).toBe(false);
      expect(fixture.componentInstance.idError()).toBe('Aadhaar must be exactly 12 digits.');
    });

    it('validates a PAN case-insensitively', () => {
      const fixture = create();
      fixture.componentInstance.idType.set('pan');
      fixture.componentInstance.idNumber.set('abcde1234f');
      expect(fixture.componentInstance.idValid()).toBe(true);
    });

    it('flags an invalid PAN', () => {
      const fixture = create();
      fixture.componentInstance.idType.set('pan');
      fixture.componentInstance.idNumber.set('123');
      expect(fixture.componentInstance.idError()).toBe('PAN must be in the format ABCDE1234F.');
    });

    it('shows no error for an empty id field', () => {
      const fixture = create();
      fixture.componentInstance.idNumber.set('');
      expect(fixture.componentInstance.idError()).toBe('');
    });
  });

  describe('submit', () => {
    it('does nothing when no customer is selected', () => {
      const fixture = create();
      fixture.componentInstance.frontFile = new File(['x'], 'a.jpg');
      fixture.componentInstance.idType.set('aadhaar');
      fixture.componentInstance.idNumber.set('123456789012');
      fixture.componentInstance.submit();
      httpMock.expectNone(() => true);
    });

    it('does nothing without a front file', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.idType.set('aadhaar');
      fixture.componentInstance.idNumber.set('123456789012');
      fixture.componentInstance.submit();
      httpMock.expectNone(() => true);
    });

    it('does nothing when the id number is invalid', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.frontFile = new File(['x'], 'a.jpg');
      fixture.componentInstance.idType.set('aadhaar');
      fixture.componentInstance.idNumber.set('123');
      fixture.componentInstance.submit();
      httpMock.expectNone(() => true);
    });

    it('uploads Aadhaar as multipart form data including the back document when present', () => {
      const fixture = create();
      const front = new File(['x'], 'front.jpg');
      const back = new File(['x'], 'back.jpg');
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.frontFile = front;
      fixture.componentInstance.backFile = back;
      fixture.componentInstance.idType.set('aadhaar');
      fixture.componentInstance.idNumber.set('123456789012');

      fixture.componentInstance.submit();

      const call = httpMock.expectOne('/api/v1/users/kyc/aadhaar');
      const body = call.request.body as FormData;
      expect(body.get('frontDocument')).toBe(front);
      expect(body.get('backDocument')).toBe(back);
      expect(body.get('customerId')).toBe('c1');
      expect(body.get('aadhaarNumber')).toBe('123456789012');
      call.flush({});

      expect(toast.success).toHaveBeenCalledWith('Aadhaar uploaded successfully');
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('uploads PAN as multipart form data', () => {
      const fixture = create();
      const front = new File(['x'], 'front.jpg');
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.frontFile = front;
      fixture.componentInstance.idType.set('pan');
      fixture.componentInstance.idNumber.set('abcde1234f');

      fixture.componentInstance.submit();

      const call = httpMock.expectOne('/api/v1/users/kyc/pan');
      const body = call.request.body as FormData;
      expect(body.get('panNumber')).toBe('ABCDE1234F');
      call.flush({});

      expect(toast.success).toHaveBeenCalledWith('PAN uploaded successfully');
    });

    it('shows an error toast and resets submitting on upload failure', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.frontFile = new File(['x'], 'front.jpg');
      fixture.componentInstance.idType.set('aadhaar');
      fixture.componentInstance.idNumber.set('123456789012');

      fixture.componentInstance.submit();
      const call = httpMock.expectOne('/api/v1/users/kyc/aadhaar');
      call.flush({ message: 'error' }, { status: 500, statusText: 'Server Error' });

      expect(toast.error).toHaveBeenCalledWith('Upload failed');
      expect(fixture.componentInstance.submitting()).toBe(false);
    });
  });
});
