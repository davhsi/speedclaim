import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationDto } from '../../../core/models/api.models';
import { TimeAgoPipe } from '../../../shared/pipes/time-ago.pipe';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';

@Component({
  selector: 'app-surveyor-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TimeAgoPipe, SafeHtmlPipe],
  templateUrl: './surveyor-layout.html',
  styles: `
    :host { display: contents; }
    .surveyor-active {
      background: rgba(245, 166, 35, 0.16) !important;
      color: #F5A623 !important;
      font-weight: 700;
      box-shadow: inset 3px 0 0 #F5A623;
    }
  `,
})
export class SurveyorLayoutComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  notifService = inject(NotificationService);

  activeTab = signal<'claims' | 'history' | 'profile'>('claims');
  sidebarCollapsed = signal(true);
  pageTitle = signal('My claims');
  notifPanelOpen = signal(false);
  profileMenuOpen = signal(false);

  readonly navItems = [
    {
      label: 'My claims',
      route: '/surveyor/claims',
      exact: false,
      icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11"/></svg>',
    },
    {
      label: 'History',
      route: '/surveyor/history',
      exact: true,
      icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>',
    },
  ];

  ngOnInit(): void {
    if (globalThis.window !== undefined && globalThis.window.innerWidth >= 1024) {
      this.sidebarCollapsed.set(false);
    }

    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
    ).subscribe(e => this.resolveTab(e.urlAfterRedirects));
    this.resolveTab(this.router.url);
    this.notifService.loadNotifications().subscribe();
  }

  private resolveTab(url: string): void {
    if (url.includes('/history')) {
      this.activeTab.set('history');
      this.pageTitle.set('Submitted reports');
    } else if (url.includes('/profile')) {
      this.activeTab.set('profile');
      this.pageTitle.set('My profile');
    } else if (url.includes('/report')) {
      this.activeTab.set('claims');
      this.pageTitle.set('Survey report');
    } else {
      this.activeTab.set('claims');
      this.pageTitle.set('My claims');
    }
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  closeSidebarOnMobile(): void {
    if (globalThis.window !== undefined && globalThis.window.innerWidth < 1024) {
      this.sidebarCollapsed.set(true);
    }
  }

  toggleNotifPanel(): void {
    this.profileMenuOpen.set(false);
    this.notifPanelOpen.update(v => !v);
  }

  toggleProfileMenu(): void {
    this.notifPanelOpen.set(false);
    this.profileMenuOpen.update(v => !v);
  }

  markAllRead(): void {
    this.notifService.markAllAsRead().subscribe();
  }

  onReadNotification(n: NotificationDto): void {
    if (!n.isRead) {
      this.notifService.markAsRead(n.id).subscribe();
    }
    if (n.redirectUrl) {
      this.notifPanelOpen.set(false);
      this.router.navigateByUrl(n.redirectUrl);
    }
  }

  userName(): string {
    const u = this.authService.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  userAvatar(): string | null {
    return this.authService.currentUser()?.avatarUrl ?? null;
  }

  logout(): void {
    this.authService.logout();
  }
}
