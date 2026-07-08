import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { PolicyDto, PremiumScheduleDto } from '../../../core/models/api.models';
import { ProductService } from '../products/services/product.service';
import { ProfileService } from '../profile/services/profile.service';
import { DashboardComponent } from './dashboard';
import { DashboardService } from './services/dashboard.service';

describe('DashboardComponent', () => {
  let dashboardService: {
    getPolicies: ReturnType<typeof vi.fn>;
    getClaims: ReturnType<typeof vi.fn>;
    getSchedule: ReturnType<typeof vi.fn>;
  };

  const activePolicy = {
    id: 'pol1',
    policyNumber: 'POL-1',
    userId: 'user1',
    status: 'Active',
    productId: 'prod1',
    productName: 'Term Life Basic',
    domain: 'Life',
    paymentFrequency: 'Monthly',
    premiumAmount: 8500,
    coverageAmount: 2500000,
    currency: 'INR',
    startDate: '2026-07-14',
    endDate: '2036-07-14',
    type: 'Individual',
  } as PolicyDto;

  function create(schedule: PremiumScheduleDto[]): ComponentFixture<DashboardComponent> {
    dashboardService = {
      getPolicies: vi.fn(() => of([activePolicy])),
      getClaims: vi.fn(() => of([])),
      getSchedule: vi.fn(() => of(schedule)),
    };

    TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: DashboardService, useValue: dashboardService },
        { provide: AuthService, useValue: { currentUser: vi.fn(() => ({ firstName: 'Deepadharsini' })) } },
        { provide: NotificationService, useValue: { unreadCount: signal(0) } },
        { provide: ProductService, useValue: { getAll: vi.fn(() => of([])) } },
        { provide: ProfileService, useValue: { getKyc: vi.fn(() => of({ kycStatus: 'Approved' })) } },
      ],
    });

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('uses the next upcoming schedule row for the premium card', () => {
    const fixture = create([
      { id: 's1', policyId: 'pol1', installmentNumber: 1, amountDue: 8500, dueDate: '2026-07-14', status: 'Paid' } as PremiumScheduleDto,
      { id: 's2', policyId: 'pol1', installmentNumber: 2, amountDue: 8500, dueDate: '2026-08-14', status: 'Upcoming' } as PremiumScheduleDto,
    ]);

    expect(dashboardService.getSchedule).toHaveBeenCalledWith('pol1');
    expect(fixture.componentInstance.nextDue()?.installmentNumber).toBe(2);
    expect(fixture.componentInstance.nextPremiumDisplay()).toBe('₹8,500');
    expect(fixture.componentInstance.nextPremiumDate()).toBe('Due: 14 Aug 2026');
  });
});
