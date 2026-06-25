import { Component, input, output, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';

interface NavItem {
  label: string;
  route: string;
  icon: string;
  exact: boolean;
  badgeCount?: number;
}

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, SafeHtmlPipe],
  templateUrl: './admin-sidebar.html',
  styles: `
    :host { display: contents; }
    .ad-active {
      background: rgba(15, 110, 140, 0.22) !important;
      color: #7DD3E8 !important;
      font-weight: 600;
    }
  `,
})
export class AdminSidebarComponent {
  collapsed = input<boolean>(true);
  userCount = input<number>(0);
  agentCount = input<number>(0);
  productCount = input<number>(0);
  toggle = output<void>();
  private authService = inject(AuthService);

  get navItems(): NavItem[] {
    return [
      { label: 'User management', route: '/admin/users', exact: false, icon: '<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>', badgeCount: this.userCount() },
      { label: 'Agent management', route: '/admin/agents', exact: false, icon: '<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 7V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v2"/><line x1="12" y1="12" x2="12" y2="16"/><line x1="10" y1="14" x2="14" y2="14"/></svg>', badgeCount: this.agentCount() },
      { label: 'Product catalog', route: '/admin/products', exact: false, icon: '<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 002 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>', badgeCount: this.productCount() },
      { label: 'System', route: '/admin/system', exact: true, icon: '<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.07 4.93a10 10 0 010 14.14M4.93 4.93a10 10 0 000 14.14M12 2v2M12 20v2M2 12h2M20 12h2"/></svg>' },
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
