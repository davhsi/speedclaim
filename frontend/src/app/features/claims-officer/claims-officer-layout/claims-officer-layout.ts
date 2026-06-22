import { Component, signal, inject, PLATFORM_ID, OnInit } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { ClaimsOfficerSidebarComponent } from '../claims-officer-sidebar/claims-officer-sidebar';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationDto } from '../../../core/models/api.models';
import { TimeAgoPipe } from '../../../shared/pipes/time-ago.pipe';

@Component({
  selector: 'app-claims-officer-layout',
  standalone: true,
  imports: [RouterOutlet, ClaimsOfficerSidebarComponent, TimeAgoPipe],
  templateUrl: './claims-officer-layout.html',
})
export class ClaimsOfficerLayoutComponent implements OnInit {
  private platformId = inject(PLATFORM_ID);
  private authService = inject(AuthService);
  private router = inject(Router);
  notifService = inject(NotificationService);

  sidebarCollapsed = signal(true);
  pageTitle = signal('Dashboard');
  notifPanelOpen = signal(false);

  private pageTitles: [string, string][] = [
    ['/claims-officer/claims/', 'Claim detail'],
    ['/claims-officer/grievances/', 'Grievance detail'],
    ['/claims-officer/dashboard', 'Dashboard'],
    ['/claims-officer/claims', 'Claims queue'],
    ['/claims-officer/grievances', 'Grievances'],
    ['/claims-officer/profile', 'My profile'],
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
  }

  private resolveTitleFromUrl(url: string): void {
    const match = this.pageTitles.find(([path]) => url.startsWith(path));
    if (match) this.pageTitle.set(match[1]);
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  toggleNotifPanel(): void {
    this.notifPanelOpen.update(v => !v);
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
