import { Component, signal, inject, PLATFORM_ID, OnInit } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet, RouterLink, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { FinanceOfficerSidebarComponent } from '../finance-officer-sidebar/finance-officer-sidebar';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { NotificationDto } from '../../../core/models/api.models';
import { TimeAgoPipe } from '../../../shared/pipes/time-ago.pipe';

@Component({
  selector: 'app-finance-officer-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, FinanceOfficerSidebarComponent, TimeAgoPipe],
  templateUrl: './finance-officer-layout.html',
})
export class FinanceOfficerLayoutComponent implements OnInit {
  private platformId = inject(PLATFORM_ID);
  private authService = inject(AuthService);
  private financeService = inject(FinanceOfficerService);
  private router = inject(Router);
  notifService = inject(NotificationService);

  sidebarCollapsed = signal(true);
  pageTitle = signal('Dashboard');
  notifPanelOpen = signal(false);
  profileMenuOpen = signal(false);
  payoutBadge = signal(0);
  commissionBadge = signal(0);

  private pageTitles: [string, string][] = [
    ['/finance-officer/dashboard', 'Dashboard'],
    ['/finance-officer/payments', 'Payment records'],
    ['/finance-officer/payouts', 'Claim payouts'],
    ['/finance-officer/commissions', 'Commissions'],
    ['/finance-officer/reports', 'Reports'],
    ['/finance-officer/profile', 'My profile'],
  ];

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId) && window.innerWidth >= 1024) {
      this.sidebarCollapsed.set(false);
    }

    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
    ).subscribe(e => this.resolveTitleFromUrl(e.urlAfterRedirects));

    this.resolveTitleFromUrl(this.router.url);
    this.notifService.loadNotifications().subscribe();
    this.loadBadgeCounts();
  }

  private resolveTitleFromUrl(url: string): void {
    const match = this.pageTitles.find(([path]) => url.startsWith(path));
    if (match) this.pageTitle.set(match[1]);
  }

  private loadBadgeCounts(): void {
    this.financeService.getClaimsForPayout('Approved').subscribe({
      next: (res) => this.payoutBadge.set(res.totalRecords),
      error: () => {},
    });

    this.financeService.getPendingCommissions().subscribe({
      next: (comms) => this.commissionBadge.set(comms.filter(c => c.status === 'Pending').length),
      error: () => {},
    });
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  toggleNotifPanel(): void {
    this.profileMenuOpen.set(false);
    this.notifPanelOpen.update(v => !v);
  }

  toggleProfileMenu(): void {
    this.notifPanelOpen.set(false);
    this.profileMenuOpen.update(v => !v);
  }

  logout(): void {
    this.authService.logout();
  }

  userName(): string {
    const u = this.authService.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  onMarkAllRead(): void {
    this.notifService.markAllAsRead().subscribe();
  }

  onReadNotification(n: NotificationDto): void {
    if (!n.isRead) {
      this.notifService.markAsRead(n.id).subscribe();
    }
  }

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
