import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { ClaimListComponent } from './claim-list';
import { ClaimsOfficerService } from '../services/claims-officer.service';
import { ClaimDto, PagedResponse } from '../../../core/models/api.models';

describe('ClaimListComponent', () => {
  let claimsService: { getAllClaims: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const claims: ClaimDto[] = [
    { id: 'c1', claimNumber: 'CL-1', customerName: 'Jane Doe', policyNumber: 'POL-1' } as ClaimDto,
    { id: 'c2', claimNumber: 'CL-2', customerName: 'Arjun Nair', policyNumber: 'POL-2' } as ClaimDto,
  ];

  function create() {
    claimsService = {
      getAllClaims: vi.fn(() => of({ data: claims, totalPages: 1, totalRecords: claims.length } as PagedResponse<ClaimDto>)),
    };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ClaimListComponent],
      providers: [
        { provide: ClaimsOfficerService, useValue: claimsService },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParams: {} } } },
      ],
    });
    const fixture = TestBed.createComponent(ClaimListComponent);
    fixture.detectChanges();
    return fixture;
  }

  describe('filteredClaims', () => {
    it('returns every loaded claim when the search box is empty', () => {
      const fixture = create();
      expect(fixture.componentInstance.filteredClaims()).toEqual(claims);
    });

    it('actually re-filters when the search term changes', () => {
      // Regression test: searchQuery used to be a plain string, so the filteredClaims
      // computed() (which only tracked the `claims` signal) never re-ran when it changed —
      // the search box looked interactive but silently did nothing.
      const fixture = create();
      fixture.componentInstance.onSearchChange('arjun');
      expect(fixture.componentInstance.filteredClaims()).toEqual([claims[1]]);
    });

    it('matches on claim number, customer name, or policy number', () => {
      const fixture = create();
      const c = fixture.componentInstance;

      c.onSearchChange('CL-1');
      expect(c.filteredClaims()).toEqual([claims[0]]);

      c.onSearchChange('pol-2');
      expect(c.filteredClaims()).toEqual([claims[1]]);
    });

    it('resets to page 1 when the search term changes', () => {
      const fixture = create();
      fixture.componentInstance.currentPage.set(3);
      fixture.componentInstance.onSearchChange('jane');
      expect(fixture.componentInstance.currentPage()).toBe(1);
    });
  });
});
