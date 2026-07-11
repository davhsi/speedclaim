import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { PolicyListComponent } from './policy-list';
import { UnderwriterService } from '../services/underwriter.service';
import { ProductService } from '../../portal/products/services/product.service';
import { PolicyDto, PagedResponse } from '../../../core/models/api.models';

describe('PolicyListComponent', () => {
  let uwService: { getAllPolicies: ReturnType<typeof vi.fn> };
  let productService: { getAll: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const policies: PolicyDto[] = [
    { id: 'p1', policyNumber: 'POL-1', status: 'Active', productName: 'Health Plus' } as PolicyDto,
    { id: 'p2', policyNumber: 'POL-2', status: 'Cancelled', productName: 'Motor Cover' } as PolicyDto,
  ];

  function create() {
    uwService = {
      getAllPolicies: vi.fn(() => of({ data: policies, pageNumber: 1, pageSize: 20, totalRecords: 2, totalPages: 1 } as PagedResponse<PolicyDto>)),
    };
    productService = { getAll: vi.fn(() => of([])) };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      imports: [PolicyListComponent],
      providers: [
        { provide: UnderwriterService, useValue: uwService },
        { provide: ProductService, useValue: productService },
        { provide: Router, useValue: router },
      ],
    });
    const fixture = TestBed.createComponent(PolicyListComponent);
    fixture.detectChanges();
    return fixture;
  }

  describe('filteredPolicies', () => {
    it('returns every loaded policy when the search box is empty', () => {
      const fixture = create();
      expect(fixture.componentInstance.filteredPolicies()).toEqual(policies);
    });

    it('actually re-filters when the search term changes', () => {
      // Regression test: searchTerm used to be a plain string with no ngModelChange handler
      // at all, so the filteredPolicies computed() (which only tracked the `allPolicies`
      // signal) never re-ran — the search box looked interactive but did nothing.
      const fixture = create();
      fixture.componentInstance.searchTerm.set('motor');
      expect(fixture.componentInstance.filteredPolicies()).toEqual([policies[1]]);
    });

    it('matches on policy number or status', () => {
      const fixture = create();
      const c = fixture.componentInstance;

      c.searchTerm.set('pol-1');
      expect(c.filteredPolicies()).toEqual([policies[0]]);

      c.searchTerm.set('cancelled');
      expect(c.filteredPolicies()).toEqual([policies[1]]);
    });
  });
});
