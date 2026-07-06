import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { KycListComponent } from './kyc-list';
import { UnderwriterService, UnderwriterKycDto } from '../services/underwriter.service';
import { PagedResponse } from '../../../core/models/api.models';

describe('KycListComponent', () => {
  let uwService: { getPendingKyc: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const record = (overrides: Partial<UnderwriterKycDto> = {}): UnderwriterKycDto => ({
    id: 'k1', userId: 'u1', kycStatus: 'Pending', aadhaarUploaded: true, panUploaded: true,
    createdAt: '2026-01-01', ...overrides,
  });

  function paged(data: UnderwriterKycDto[], page = 1, totalPages = 1): PagedResponse<UnderwriterKycDto> {
    return { data, pageNumber: page, pageSize: 10, totalRecords: data.length, totalPages };
  }

  function create(response: PagedResponse<UnderwriterKycDto> = paged([record()])) {
    uwService.getPendingKyc.mockReturnValue(of(response));
    const fixture = TestBed.createComponent(KycListComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    uwService = { getPendingKyc: vi.fn() };
    router = { navigate: vi.fn() };
    TestBed.configureTestingModule({
      imports: [KycListComponent],
      providers: [
        { provide: UnderwriterService, useValue: uwService },
        { provide: Router, useValue: router },
      ],
    });
  });

  describe('ngOnInit', () => {
    it('loads page 1 with the default page size', () => {
      const fixture = create(paged([record()]));
      expect(uwService.getPendingKyc).toHaveBeenCalledWith(1, 10);
      expect(fixture.componentInstance.kycRecords()).toHaveLength(1);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('adopts pagination metadata from the response', () => {
      const fixture = create(paged([record()], 1, 3));
      expect(fixture.componentInstance.totalPages()).toBe(3);
    });

    it('stops loading even if the fetch fails', () => {
      uwService.getPendingKyc.mockReturnValue(throwError(() => ({ status: 500 })));
      const fixture = TestBed.createComponent(KycListComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('onPageChange', () => {
    it('reloads the given page', () => {
      const fixture = create(paged([record()], 1, 2));
      uwService.getPendingKyc.mockReturnValue(of(paged([record({ id: 'k2' })], 2, 2)));

      fixture.componentInstance.onPageChange(2);

      expect(uwService.getPendingKyc).toHaveBeenCalledWith(2, 10);
      expect(fixture.componentInstance.currentPage()).toBe(2);
      expect(fixture.componentInstance.kycRecords()[0].id).toBe('k2');
    });
  });

  describe('openKyc', () => {
    it('navigates to the detail route for the given userId', () => {
      const fixture = create();
      fixture.componentInstance.openKyc('u1');
      expect(router.navigate).toHaveBeenCalledWith(['/underwriter/kyc', 'u1']);
    });
  });
});
