import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { SurveyorProfileComponent } from './surveyor-profile';
import { SurveyorProfileDto, SurveyorService } from '../services/surveyor.service';
import { AuthService } from '../../../core/services/auth.service';
import { AuthUserDto, ClaimDto } from '../../../core/models/api.models';

describe('SurveyorProfileComponent', () => {
  let surveyorService: { getProfile: ReturnType<typeof vi.fn>; getAssignedClaims: ReturnType<typeof vi.fn> };
  let authService: { currentUser: ReturnType<typeof vi.fn>; logout: ReturnType<typeof vi.fn> };

  const baseProfile: SurveyorProfileDto = {
    surveyorId: 's1', userId: 'u1', email: 'surveyor@example.com', fullName: 'Sam Surveyor',
    phone: '9876543210', licenseNumber: 'LIC-1', licenseExpiry: '2030-01-01',
    specialization: 'Motor', surveyorType: 'Internal', isActive: true,
  };

  function create(profile: SurveyorProfileDto | null = baseProfile, claims: ClaimDto[] = []) {
    surveyorService.getProfile.mockReturnValue(profile ? of(profile) : throwError(() => ({ status: 404 })));
    surveyorService.getAssignedClaims.mockReturnValue(of(claims));
    const fixture = TestBed.createComponent(SurveyorProfileComponent);
    fixture.detectChanges();
    return fixture;
  }

  function claim(status: ClaimDto['status'], intimationDate: string): ClaimDto {
    return { status, intimationDate } as ClaimDto;
  }

  beforeEach(() => {
    surveyorService = { getProfile: vi.fn(), getAssignedClaims: vi.fn() };
    authService = { currentUser: vi.fn(() => ({ firstName: 'Sam', lastName: 'Surveyor' }) as AuthUserDto), logout: vi.fn() };

    TestBed.configureTestingModule({
      imports: [SurveyorProfileComponent],
      providers: [
        { provide: SurveyorService, useValue: surveyorService },
        { provide: AuthService, useValue: authService },
      ],
    });
  });

  describe('ngOnInit', () => {
    it('loads the profile and assigned claims', () => {
      const fixture = create(baseProfile, [claim('Approved', '2024-01-01')]);
      expect(fixture.componentInstance.profile()).toEqual(baseProfile);
      expect(fixture.componentInstance.claims()).toHaveLength(1);
    });

    it('swallows a profile fetch failure without throwing', () => {
      expect(() => create(null)).not.toThrow();
      expect(create(null).componentInstance.profile()).toBeNull();
    });
  });

  describe('display computed signals', () => {
    it('derives fullName/initials from the current user', () => {
      const c = create().componentInstance;
      expect(c.fullName()).toBe('Sam Surveyor');
      expect(c.initials()).toBe('SS');
    });

    it('falls back to "Surveyor"/"?" with no current user', () => {
      authService.currentUser.mockReturnValue(null);
      const c = create().componentInstance;
      expect(c.fullName()).toBe('Surveyor');
      expect(c.initials()).toBe('?');
    });

    it('prefers the profile email/phone over the current user, with a fallback chain', () => {
      const c = create({ ...baseProfile, email: 'from-profile@example.com', phone: '1111111111' }).componentInstance;
      expect(c.email()).toBe('from-profile@example.com');
      expect(c.phone()).toBe('1111111111');
    });

    it('defaults licenseNo/specialization/isActive when there is no profile', () => {
      const c = create(null).componentInstance;
      expect(c.licenseNo()).toBe('Not assigned');
      expect(c.specialization()).toBe('Assigned Region');
      expect(c.isActive()).toBe(true);
    });
  });

  describe('claim-derived computed signals', () => {
    it('counts total and submitted (Settled/Approved) claims', () => {
      const c = create(baseProfile, [
        claim('Approved', '2024-01-01'),
        claim('Settled', '2024-01-01'),
        claim('UnderReview', '2024-01-01'),
      ]).componentInstance;
      expect(c.totalClaims()).toBe(3);
      expect(c.submittedCount()).toBe(2);
    });

    it('counts overdue claims: not settled/approved and intimated over 7 days ago', () => {
      const longAgo = '2000-01-01';
      const c = create(baseProfile, [
        claim('UnderReview', longAgo),
        claim('DocumentsPending', longAgo),
        claim('Approved', longAgo), // excluded: already approved
      ]).componentInstance;
      expect(c.overdueCount()).toBe(2);
    });

    it('does not count a recently intimated pending claim as overdue', () => {
      const today = new Date().toISOString();
      const c = create(baseProfile, [claim('UnderReview', today)]).componentInstance;
      expect(c.overdueCount()).toBe(0);
    });
  });

  it('signOut delegates to authService.logout', () => {
    const c = create().componentInstance;
    c.signOut();
    expect(authService.logout).toHaveBeenCalled();
  });
});
