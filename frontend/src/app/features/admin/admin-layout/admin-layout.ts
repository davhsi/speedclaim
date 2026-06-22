import { Component, signal, inject, PLATFORM_ID, OnInit } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { AdminSidebarComponent } from '../admin-sidebar/admin-sidebar';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AdminService } from '../services/admin.service';
import { NotificationDto } from '../../../core/models/api.models';
import { TimeAgoPipe } from '../../../shared/pipes/time-ago.pipe';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, AdminSidebarComponent, TimeAgoPipe],
  templateUrl: './admin-layout.html',
})
export class AdminLayoutComponent implements OnInit {
  private platformId = inject(PLATFORM_ID);
  private authService = inject(AuthService);
  private adminService = inject(AdminService);
  private router = inject(Router);
  notifService = inject(NotificationService);

  sidebarCollapsed = signal(true);
  pageTitle = signal('User management');
  notifPanelOpen = signal(false);
  userCount = signal(0);
  agentCount = signal(0);
  productCount = signal(0);

  private pageTitles: [string, string][] = [
    ['/admin/users', 'User management'],
    ['/admin/agents', 'Agent management'],
    ['/admin/products', 'Product catalog'],
    ['/admin/system', 'System'],
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
    this.loadCounts();
  }

  private resolveTitleFromUrl(url: string): void {
    const match = this.pageTitles.find(([path]) => url.startsWith(path));
    if (match) this.pageTitle.set(match[1]);
  }

  private loadCounts(): void {
    this.adminService.getAllUsers(1, 1).subscribe({
      next: (res) => this.userCount.set(res.totalRecords),
    });
    this.adminService.getAgentProfiles().subscribe({
      next: (agents) => this.agentCount.set(agents.length),
    });
    this.adminService.getProducts().subscribe({
      next: (products) => this.productCount.set(products.length),
    });
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
