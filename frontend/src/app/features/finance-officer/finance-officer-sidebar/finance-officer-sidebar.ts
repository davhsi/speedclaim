import { Component, input, output, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

interface NavItem {
  label: string;
  route: string;
  icon: string;
  exact: boolean;
  badgeCount?: number;
}

@Component({
  selector: 'app-finance-officer-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './finance-officer-sidebar.html',
  styles: `
    :host { display: contents; }
    .fo-active {
      background: rgba(15, 110, 140, 0.18) !important;
      color: #fff !important;
      font-weight: 600;
    }
  `,
})
export class FinanceOfficerSidebarComponent {
  collapsed = input<boolean>(true);
  payoutBadge = input<number>(0);
  commissionBadge = input<number>(0);
  toggle = output<void>();
  private authService = inject(AuthService);

  get navItems(): NavItem[] {
    return [
      { label: 'Dashboard', route: '/finance-officer/dashboard', exact: true, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>' },
      { label: 'Payment records', route: '/finance-officer/payments', exact: true, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="1" y="4" width="22" height="16" rx="2" ry="2"/><line x1="1" y1="10" x2="23" y2="10"/></svg>' },
      { label: 'Claim payouts', route: '/finance-officer/payouts', exact: true, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a1 1 0 11-2 0 1 1 0 012 0z"/></svg>', badgeCount: this.payoutBadge() },
      { label: 'Commissions', route: '/finance-officer/commissions', exact: true, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><line x1="19" y1="5" x2="5" y2="19"/><circle cx="6.5" cy="6.5" r="2.5"/><circle cx="17.5" cy="17.5" r="2.5"/></svg>', badgeCount: this.commissionBadge() },
      { label: 'Reports', route: '/finance-officer/reports', exact: true, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>' },
    ];
  }

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  userName(): string {
    const u = this.authService.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  logout(): void {
    this.authService.logout();
  }

  onNavClick(): void {
    if (typeof window !== 'undefined' && window.innerWidth < 1024) {
      this.toggle.emit();
    }
  }
}
