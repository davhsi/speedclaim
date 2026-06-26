import { Component, inject, output } from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, filter } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './topbar.html',
})
export class TopbarComponent {
  menuToggle = output<void>();

  private authService = inject(AuthService);
  private router = inject(Router);
  notifService = inject(NotificationService);

  private routeTitleMap: Record<string, string> = {
    '/dashboard': 'Dashboard',
    '/products': 'Browse Products',
    '/quote': 'Get a Quote',
    '/proposals': 'My Proposals',
    '/policies': 'My Policies',
    '/claims': 'My Claims',
    '/claims/new': 'File a Claim',
    '/payments': 'Payment History',
    '/notifications': 'Notifications',
    '/grievances': 'Grievances',
    '/kyc': 'KYC Verification',
    '/profile': 'My Profile',
  };

  pageTitle = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => {
        const path = e.urlAfterRedirects.split('?')[0];
        return this.routeTitleMap[path] ?? this.deriveTitleFromPath(path);
      }),
    ),
    { initialValue: 'Dashboard' },
  );

  userInitial(): string {
    const u = this.authService.currentUser();
    return u ? u.firstName.charAt(0).toUpperCase() : '?';
  }

  private deriveTitleFromPath(path: string): string {
    const segments = path.split('/').filter(Boolean);
    const isId = (s: string) =>
      /^\d+$/.test(s) ||
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(s);
    if (segments.length >= 2 && isId(segments[segments.length - 1])) {
      const base = segments[segments.length - 2];
      const singularMap: Record<string, string> = {
        products: 'Product', policies: 'Policy', claims: 'Claim',
        proposals: 'Proposal', grievances: 'Grievance',
      };
      const label = singularMap[base] ?? (base.charAt(0).toUpperCase() + base.slice(1).replace(/-/g, ' '));
      return label + ' Details';
    }
    if (segments.length >= 1) {
      const base = segments[segments.length - 1];
      return base.charAt(0).toUpperCase() + base.slice(1).replace(/-/g, ' ');
    }
    return 'Dashboard';
  }
}
