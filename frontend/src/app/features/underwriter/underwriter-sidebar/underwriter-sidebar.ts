import { Component, input, output, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';

interface NavItem {
  label: string;
  route: string;
  icon: string;
  exact: boolean;
}

@Component({
  selector: 'app-underwriter-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, SafeHtmlPipe],
  templateUrl: './underwriter-sidebar.html',
  styles: `
    :host { display: contents; }
    .uw-active {
      background: rgba(245, 166, 35, 0.16) !important;
      color: #F5A623 !important;
      font-weight: 700;
      box-shadow: inset 3px 0 0 #F5A623;
    }
  `,
})
export class UnderwriterSidebarComponent {
  collapsed = input<boolean>(true);
  sidebarToggle = output<void>();
  private readonly authService = inject(AuthService);

  navItems: NavItem[] = [
    { label: 'Dashboard', route: '/underwriter/dashboard', exact: true, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>' },
    { label: 'Proposals', route: '/underwriter/proposals', exact: false, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>' },
    { label: 'KYC review', route: '/underwriter/kyc', exact: false, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="8.5" cy="7" r="4"/><polyline points="17 11 19 13 23 7"/></svg>' },
    { label: 'Endorsements', route: '/underwriter/endorsements', exact: false, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 013 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>' },
    { label: 'All policies', route: '/underwriter/policies', exact: false, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 016.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z"/></svg>' },
  ];

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  userAvatar(): string | null {
    return this.authService.currentUser()?.avatarUrl ?? null;
  }

  userName(): string {
    const u = this.authService.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  logout(): void {
    this.authService.logout();
  }

  onNavClick(): void {
    if (typeof globalThis.window !== 'undefined' && globalThis.window.innerWidth < 1024) {
      this.sidebarToggle.emit();
    }
  }
}
