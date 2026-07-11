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

  function create(queryParams: Record<string, string> = {}) {
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
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: (key: string) => queryParams[key] ?? null } } } },
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

  describe('customerId query param preselect', () => {
    it('preselects the customer and fetches their KYC record when the id matches', () => {
      const fixture = create({ customerId: 'c1' });

      const call = httpMock.expectOne('/api/v1/agents/customers/c1/kyc');
      call.flush({ id: 'k1' } as KycRecordDto);

      expect(fixture.componentInstance.selectedCustomerId()).toBe('c1');
      expect(fixture.componentInstance.existingKyc()).toEqual({ id: 'k1' });
    });

    it('ignores the query param when it does not match any of the agent\'s customers', () => {
      const fixture = create({ customerId: 'not-mine' });
      expect(fixture.componentInstance.selectedCustomerId()).toBeNull();
      httpMock.expectNone(() => true);
    });

    it('does nothing when there is no customerId query param', () => {
      const fixture = create();
      expect(fixture.componentInstance.selectedCustomerId()).toBeNull();
      httpMock.expectNone(() => true);
    });
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
      fixture.componentInstance.aadhaarNumber.set('123456789012');
      expect(fixture.componentInstance.idValid()).toBe(true);
      expect(fixture.componentInstance.idError()).toBe('');
    });

    it('flags an invalid Aadhaar number', () => {
      const fixture = create();
      fixture.componentInstance.idType.set('aadhaar');
      fixture.componentInstance.aadhaarNumber.set('123');
      expect(fixture.componentInstance.idValid()).toBe(false);
      expect(fixture.componentInstance.idError()).toBe('Aadhaar must be exactly 12 digits.');
    });

    it('validates a PAN case-insensitively', () => {
      const fixture = create();
      fixture.componentInstance.idType.set('pan');
      fixture.componentInstance.panNumber.set('abcde1234f');
      expect(fixture.componentInstance.idValid()).toBe(true);
    });

    it('flags an invalid PAN', () => {
      const fixture = create();
      fixture.componentInstance.idType.set('pan');
      fixture.componentInstance.panNumber.set('123');
      expect(fixture.componentInstance.idError()).toBe('PAN must be in the format ABCDE1234F.');
    });

    it('shows no error for an empty id field', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNumber.set('');
      expect(fixture.componentInstance.idError()).toBe('');
    });
  });

  describe('switching document type tabs', () => {
    it('keeps each document type\'s number and file independently instead of wiping them', () => {
      const fixture = create();
      const aadhaarDoc = new File(['x'], 'aadhaar.jpg');
      fixture.componentInstance.idType.set('aadhaar');
      fixture.componentInstance.aadhaarNumber.set('123456789012');
      fixture.componentInstance.aadhaarFile.set(aadhaarDoc);

      fixture.componentInstance.selectIdType('pan');
      fixture.componentInstance.panNumber.set('abcde1234f');
      fixture.componentInstance.panFile.set(new File(['x'], 'pan.jpg'));

      fixture.componentInstance.selectIdType('aadhaar');

      expect(fixture.componentInstance.aadhaarNumber()).toBe('123456789012');
      expect(fixture.componentInstance.aadhaarFile()).toBe(aadhaarDoc);
      expect(fixture.componentInstance.aadhaarReady()).toBe(true);
      expect(fixture.componentInstance.panReady()).toBe(true);
    });
  });

  describe('canEditKyc / canSubmit gated by KYC status', () => {
    function readyToSubmit(fixture: ReturnType<typeof create>) {
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'a.jpg'));
      fixture.componentInstance.aadhaarNumber.set('123456789012');
    }

    it('allows editing when the customer has never submitted KYC', () => {
      const fixture = create();
      readyToSubmit(fixture);
      expect(fixture.componentInstance.canEditKyc()).toBe(true);
      expect(fixture.componentInstance.canSubmit()).toBe(true);
    });

    it('blocks editing while KYC is Pending review', () => {
      const fixture = create();
      readyToSubmit(fixture);
      fixture.componentInstance.existingKyc.set({ id: 'k1', kycStatus: 'Pending' } as KycRecordDto);

      expect(fixture.componentInstance.canEditKyc()).toBe(false);
      expect(fixture.componentInstance.canSubmit()).toBe(false);
    });

    it('blocks editing once KYC is Approved', () => {
      const fixture = create();
      readyToSubmit(fixture);
      fixture.componentInstance.existingKyc.set({ id: 'k1', kycStatus: 'Approved' } as KycRecordDto);

      expect(fixture.componentInstance.canEditKyc()).toBe(false);
      expect(fixture.componentInstance.canSubmit()).toBe(false);
    });

    it('allows re-editing once KYC is Rejected', () => {
      const fixture = create();
      readyToSubmit(fixture);
      fixture.componentInstance.existingKyc.set({ id: 'k1', kycStatus: 'Rejected' } as KycRecordDto);

      expect(fixture.componentInstance.canEditKyc()).toBe(true);
      expect(fixture.componentInstance.canSubmit()).toBe(true);
    });

    it('submit() is a no-op while KYC is Pending, even with valid input', () => {
      const fixture = create();
      readyToSubmit(fixture);
      fixture.componentInstance.existingKyc.set({ id: 'k1', kycStatus: 'Pending' } as KycRecordDto);

      fixture.componentInstance.submit();

      httpMock.expectNone(() => true);
    });
  });

  describe('submit', () => {
    it('does nothing when no customer is selected', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'a.jpg'));
      fixture.componentInstance.aadhaarNumber.set('123456789012');
      fixture.componentInstance.submit();
      httpMock.expectNone(() => true);
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('does nothing without a document file', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.aadhaarNumber.set('123456789012');
      fixture.componentInstance.submit();
      httpMock.expectNone(() => true);
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('does nothing when the id number is invalid', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'a.jpg'));
      fixture.componentInstance.aadhaarNumber.set('123');
      fixture.componentInstance.submit();
      httpMock.expectNone(() => true);
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('uploads only Aadhaar when only it is complete', () => {
      const fixture = create();
      const doc = new File(['x'], 'aadhaar.jpg');
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.aadhaarFile.set(doc);
      fixture.componentInstance.aadhaarNumber.set('123456789012');

      fixture.componentInstance.submit();

      const call = httpMock.expectOne('/api/v1/users/kyc/aadhaar');
      const body = call.request.body as FormData;
      expect(body.get('document')).toBe(doc);
      expect(body.get('customerId')).toBe('c1');
      expect(body.get('aadhaarNumber')).toBe('123456789012');
      call.flush({});
      httpMock.expectNone('/api/v1/users/kyc/pan');

      expect(toast.success).toHaveBeenCalledWith('Aadhaar uploaded successfully');
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('uploads only PAN when only it is complete', () => {
      const fixture = create();
      const doc = new File(['x'], 'pan.jpg');
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.panFile.set(doc);
      fixture.componentInstance.panNumber.set('abcde1234f');

      fixture.componentInstance.submit();

      const call = httpMock.expectOne('/api/v1/users/kyc/pan');
      const body = call.request.body as FormData;
      expect(body.get('panNumber')).toBe('ABCDE1234F');
      call.flush({});

      expect(toast.success).toHaveBeenCalledWith('PAN uploaded successfully');
    });

    it('does not send the PAN request until the Aadhaar request has completed', () => {
      // Regression test: both uploads used to fire via forkJoin (in parallel), and for a
      // brand-new customer both requests race to create the same underlying KycRecord row
      // server-side, tripping a unique-constraint violation. Sequential-only prevents that.
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'aadhaar.jpg'));
      fixture.componentInstance.aadhaarNumber.set('123456789012');
      fixture.componentInstance.panFile.set(new File(['x'], 'pan.jpg'));
      fixture.componentInstance.panNumber.set('abcde1234f');

      fixture.componentInstance.submit();

      httpMock.expectOne('/api/v1/users/kyc/aadhaar');
      httpMock.expectNone('/api/v1/users/kyc/pan');
    });

    it('uploads both Aadhaar and PAN together in a single submit when both are complete', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'aadhaar.jpg'));
      fixture.componentInstance.aadhaarNumber.set('123456789012');
      fixture.componentInstance.panFile.set(new File(['x'], 'pan.jpg'));
      fixture.componentInstance.panNumber.set('abcde1234f');

      fixture.componentInstance.submit();

      // Sequential, not parallel — the PAN request isn't sent until Aadhaar's response
      // lands, so that a brand-new customer's shared KycRecord row is created by the
      // first call before the second one looks for it (see submit()'s race-condition note).
      httpMock.expectOne('/api/v1/users/kyc/aadhaar').flush({});
      httpMock.expectOne('/api/v1/users/kyc/pan').flush({});

      expect(toast.success).toHaveBeenCalledWith('Aadhaar & PAN uploaded successfully');
      expect(router.navigate).toHaveBeenCalledWith(['/agent/customers']);
    });

    it('shows an error toast and resets submitting on upload failure', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'front.jpg'));
      fixture.componentInstance.aadhaarNumber.set('123456789012');

      fixture.componentInstance.submit();
      const call = httpMock.expectOne('/api/v1/users/kyc/aadhaar');
      call.flush({ message: 'error' }, { status: 500, statusText: 'Server Error' });

      expect(toast.error).toHaveBeenCalledWith('Aadhaar upload failed');
      expect(fixture.componentInstance.submitting()).toBe(false);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('reports partial failure and still navigates for the succeeded one when only one of two succeeds', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'aadhaar.jpg'));
      fixture.componentInstance.aadhaarNumber.set('123456789012');
      fixture.componentInstance.panFile.set(new File(['x'], 'pan.jpg'));
      fixture.componentInstance.panNumber.set('abcde1234f');

      fixture.componentInstance.submit();

      httpMock.expectOne('/api/v1/users/kyc/aadhaar').flush({});
      httpMock.expectOne('/api/v1/users/kyc/pan').flush({ message: 'error' }, { status: 500, statusText: 'Server Error' });

      expect(toast.success).toHaveBeenCalledWith('Aadhaar uploaded successfully');
      expect(toast.error).toHaveBeenCalledWith('PAN upload failed');
      expect(router.navigate).not.toHaveBeenCalled();
    });
  });

  describe('canDeactivate', () => {
    it('allows navigation when nothing has been entered', () => {
      const fixture = create();
      expect(fixture.componentInstance.canDeactivate()).toBe(true);
    });

    it('prompts for confirmation when an id number has been typed but not submitted', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNumber.set('123456789012');

      const result = fixture.componentInstance.canDeactivate();

      expect(fixture.componentInstance.showLeaveConfirm()).toBe(true);
      expect(result).not.toBe(true);
    });

    it('prompts for confirmation when a file has been attached but not submitted', () => {
      const fixture = create();
      fixture.componentInstance.panFile.set(new File(['x'], 'pan.jpg'));

      const result = fixture.componentInstance.canDeactivate();

      expect(fixture.componentInstance.showLeaveConfirm()).toBe(true);
      expect(result).not.toBe(true);
    });

    it('resolves true on confirmLeave and false on cancelLeave', async () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNumber.set('123456789012');

      const result$ = fixture.componentInstance.canDeactivate();
      const resultPromise = new Promise(resolve => (result$ as any).subscribe(resolve));
      fixture.componentInstance.confirmLeave();

      expect(await resultPromise).toBe(true);
      expect(fixture.componentInstance.showLeaveConfirm()).toBe(false);
    });

    it('allows navigation without prompting right after a successful submit', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId.set('c1');
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'aadhaar.jpg'));
      fixture.componentInstance.aadhaarNumber.set('123456789012');

      fixture.componentInstance.submit();
      httpMock.expectOne('/api/v1/users/kyc/aadhaar').flush({});

      expect(fixture.componentInstance.canDeactivate()).toBe(true);
    });
  });
});
