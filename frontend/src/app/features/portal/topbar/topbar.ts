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
    if (segments.length >= 2 && /^\d+$/.test(segments[segments.length - 1])) {
      const base = segments[segments.length - 2];
      return base.charAt(0).toUpperCase() + base.slice(1).replace(/-/g, ' ') + ' Details';
    }
    if (segments.length >= 1) {
      const base = segments[segments.length - 1];
      return base.charAt(0).toUpperCase() + base.slice(1).replace(/-/g, ' ');
    }
    return 'Dashboard';
  }
}
