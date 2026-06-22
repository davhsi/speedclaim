import { Component, signal, inject, PLATFORM_ID, OnInit } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { AgentSidebarComponent } from '../agent-sidebar/agent-sidebar';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-agent-layout',
  standalone: true,
  imports: [RouterOutlet, AgentSidebarComponent],
  templateUrl: './agent-layout.html',
})
export class AgentLayoutComponent implements OnInit {
  private platformId = inject(PLATFORM_ID);
  private authService = inject(AuthService);
  private router = inject(Router);

  sidebarCollapsed = signal(true);
  pageTitle = signal('Dashboard');

  private pageTitles: [string, string][] = [
    ['/agent/proposals/new', 'Submit Proposal'],
    ['/agent/dashboard', 'Dashboard'],
    ['/agent/customers', 'My Customers'],
    ['/agent/proposals', 'My Proposals'],
    ['/agent/policies', 'Customer Policies'],
    ['/agent/renewals', 'Renewals'],
    ['/agent/commissions', 'Commissions'],
  ];

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId) && window.innerWidth >= 1024) {
      this.sidebarCollapsed.set(false);
    }

    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
    ).subscribe(e => this.resolveTitleFromUrl(e.urlAfterRedirects));

    this.resolveTitleFromUrl(this.router.url);
  }

  private resolveTitleFromUrl(url: string): void {
    const match = this.pageTitles.find(([path]) => url.startsWith(path));
    if (match) this.pageTitle.set(match[1]);
    else if (url.startsWith('/agent/customers/')) this.pageTitle.set('Customer Details');
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }
}
