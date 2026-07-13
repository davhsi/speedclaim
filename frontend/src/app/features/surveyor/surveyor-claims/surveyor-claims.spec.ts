import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SurveyorClaimsComponent } from './surveyor-claims';
import { SurveyorLayoutComponent } from '../surveyor-layout/surveyor-layout';
import { AuthService } from '../../../core/services/auth.service';
import { SurveyorService } from '../services/surveyor.service';
import { ClaimDto } from '../../../core/models/api.models';

describe('SurveyorClaimsComponent', () => {
  let surveyorService: { getAssignedClaims: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let layoutStub: { openNotifications: ReturnType<typeof vi.fn>; notifService: { unreadCount: () => number }; userInitials: () => string };

  function daysAgo(n: number): string {
    return new Date(Date.now() - n * 86400000).toISOString();
  }

  function claim(overrides: Partial<ClaimDto> = {}): ClaimDto {
    return { id: 'c1', status: 'UnderReview', intimationDate: daysAgo(1), ...overrides } as ClaimDto;
  }

  function create(claims: ClaimDto[] = [claim()]) {
    surveyorService.getAssignedClaims.mockReturnValue(of(claims));
    TestBed.configureTestingModule({
      imports: [SurveyorClaimsComponent],
      providers: [
        { provide: SurveyorLayoutComponent, useValue: layoutStub },
        { provide: AuthService, useValue: { currentUser: () => null } },
        { provide: SurveyorService, useValue: surveyorService },
        { provide: Router, useValue: router },
      ],
    });
    const fixture = TestBed.createComponent(SurveyorClaimsComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    surveyorService = { getAssignedClaims: vi.fn() };
    router = { navigate: vi.fn() };
    layoutStub = { openNotifications: vi.fn(), notifService: { unreadCount: () => 0 }, userInitials: () => 'JD' };
  });

  describe('ngOnInit', () => {
    it('loads assigned claims and stops loading', () => {
      const fixture = create([claim({ id: 'c1' }), claim({ id: 'c2' })]);
      expect(fixture.componentInstance.claims()).toHaveLength(2);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('stops loading even when the request fails', () => {
      surveyorService.getAssignedClaims.mockReturnValue(throwError(() => ({ status: 500 })));
      TestBed.configureTestingModule({
        imports: [SurveyorClaimsComponent],
        providers: [
          { provide: SurveyorLayoutComponent, useValue: layoutStub },
          { provide: AuthService, useValue: { currentUser: () => null } },
          { provide: SurveyorService, useValue: surveyorService },
          { provide: Router, useValue: router },
        ],
      });
      const fixture = TestBed.createComponent(SurveyorClaimsComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('mapSurveyStatus', () => {
    it('maps Settled/Approved/Withdrawn/Rejected to Submitted', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      for (const status of ['Settled', 'Approved', 'Withdrawn', 'Rejected'] as const) {
        expect(c.mapSurveyStatus(claim({ status }))).toBe('Submitted');
      }
    });

    it('maps a claim with submitted survey fields to Submitted even while UnderReview', () => {
      const fixture = create();
      expect(fixture.componentInstance.mapSurveyStatus(claim({
        status: 'UnderReview',
        surveyDate: new Date().toISOString(),
        surveyEstimatedCost: 8500,
      }))).toBe('Submitted');
    });

    it('maps a claim intimated more than 7 days ago to Overdue', () => {
      const fixture = create();
      expect(fixture.componentInstance.mapSurveyStatus(claim({ status: 'UnderReview', intimationDate: daysAgo(8) }))).toBe('Overdue');
    });

    it('maps a claim intimated within 7 days to Pending', () => {
      const fixture = create();
      expect(fixture.componentInstance.mapSurveyStatus(claim({ status: 'UnderReview', intimationDate: daysAgo(2) }))).toBe('Pending');
    });
  });

  describe('tab filtering and counts', () => {
    it('computes pending/overdue/submitted counts across all claims', () => {
      const fixture = create([
        claim({ id: 'c1', status: 'UnderReview', intimationDate: daysAgo(1) }), // Pending
        claim({ id: 'c2', status: 'UnderReview', intimationDate: daysAgo(10) }), // Overdue
        claim({ id: 'c3', status: 'Settled' }), // Submitted
      ]);
      const c = fixture.componentInstance;
      expect(c.pendingCount()).toBe(1);
      expect(c.overdueCount()).toBe(1);
      expect(c.submittedCount()).toBe(1);
    });

    it('filteredClaims returns everything for the "all" tab', () => {
      const fixture = create([claim({ id: 'c1' }), claim({ id: 'c2', status: 'Settled' })]);
      expect(fixture.componentInstance.filteredClaims()).toHaveLength(2);
    });

    it('onTabChange filters to the selected tab and resets to page 1', () => {
      const fixture = create([
        claim({ id: 'c1', status: 'UnderReview', intimationDate: daysAgo(1) }),
        claim({ id: 'c2', status: 'Settled' }),
      ]);
      const c = fixture.componentInstance;
      c.currentPage.set(2);

      c.onTabChange('submitted');

      expect(c.activeTab()).toBe('submitted');
      expect(c.currentPage()).toBe(1);
      expect(c.filteredClaims()).toHaveLength(1);
      expect(c.filteredClaims()[0].id).toBe('c2');
    });
  });

  describe('pagination', () => {
    it('paginates the filtered list at 10 per page', () => {
      const list = Array.from({ length: 15 }, (_, i) => claim({ id: `c${i}`, intimationDate: daysAgo(1) }));
      const fixture = create(list);
      expect(fixture.componentInstance.totalPages()).toBe(2);
      expect(fixture.componentInstance.pagedClaims()).toHaveLength(10);
      fixture.componentInstance.onPageChange(2);
      expect(fixture.componentInstance.pagedClaims()).toHaveLength(5);
    });
  });

  describe('openClaim', () => {
    it('navigates to the report page for a non-submitted claim', () => {
      const fixture = create();
      fixture.componentInstance.openClaim(claim({ id: 'c1', status: 'UnderReview', intimationDate: daysAgo(1) }));
      expect(router.navigate).toHaveBeenCalledWith(['/surveyor/claims', 'c1', 'report']);
    });

    it('does not navigate for an already-submitted claim', () => {
      const fixture = create();
      fixture.componentInstance.openClaim(claim({ id: 'c1', status: 'Settled' }));
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('does not navigate when a survey report has already been submitted', () => {
      const fixture = create();
      fixture.componentInstance.openClaim(claim({
        id: 'c1',
        status: 'UnderReview',
        surveyDate: new Date().toISOString(),
      }));
      expect(router.navigate).not.toHaveBeenCalled();
    });
  });

  describe('display helpers', () => {
    it('barColor/statusClasses/displayStatus are consistent with mapSurveyStatus', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      expect(c.displayStatus('Settled')).toBe('Submitted');
      expect(c.barColor('Settled')).toBe('#1F9D6B');
      expect(c.statusClasses('Settled')).toContain('bg-success-bg');
    });
  });

  describe('formatINR', () => {
    it('formats with Indian digit grouping', () => {
      const fixture = create();
      expect(fixture.componentInstance.formatINR(1234567)).toBe('₹12,34,567.00');
    });
  });
});
