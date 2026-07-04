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
  selector: 'app-claims-officer-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, SafeHtmlPipe],
  templateUrl: './claims-officer-sidebar.html',
  styles: `
    :host { display: contents; }
    .co-active {
      background: rgba(245, 166, 35, 0.16) !important;
      color: #F5A623 !important;
      font-weight: 700;
      box-shadow: inset 3px 0 0 #F5A623;
    }
  `,
})
export class ClaimsOfficerSidebarComponent {
  collapsed = input<boolean>(true);
  sidebarToggle = output<void>();
  private readonly authService = inject(AuthService);

  navItems: NavItem[] = [
    { label: 'Dashboard', route: '/claims-officer/dashboard', exact: true, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>' },
    { label: 'Claims', route: '/claims-officer/claims', exact: false, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>' },
    { label: 'Grievances', route: '/claims-officer/grievances', exact: false, icon: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z"/></svg>' },
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
    if (globalThis.window !== undefined && globalThis.window.innerWidth < 1024) {
      this.sidebarToggle.emit();
    }
  }
}
