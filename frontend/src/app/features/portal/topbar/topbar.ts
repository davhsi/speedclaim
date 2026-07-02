import { Component, inject, output, signal, computed, OnInit } from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, filter } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { TimeAgoPipe } from '../../../shared/pipes/time-ago.pipe';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [RouterLink, TimeAgoPipe],
  templateUrl: './topbar.html',
})
export class TopbarComponent implements OnInit {
  menuToggle = output<void>();

  private authService = inject(AuthService);
  private router = inject(Router);
  notifService = inject(NotificationService);
  profileMenuOpen = signal(false);
  notifOpen = signal(false);

  recentNotifs = computed(() => this.notifService.notifications().slice(0, 5));

  ngOnInit(): void {
    this.notifService.loadNotifications().subscribe();
  }

  toggleNotif(): void { this.notifOpen.update(v => !v); }
  closeNotif(): void { this.notifOpen.set(false); }

  markReadAndNavigate(id: string): void {
    this.notifService.markAsRead(id).subscribe();
    this.notifOpen.set(false);
    this.router.navigate(['/notifications']);
  }

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

  avatarUrl(): string | null {
    return this.authService.currentUser()?.avatarUrl ?? null;
  }

  userName(): string {
    const u = this.authService.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  logout(): void {
    this.authService.logout();
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
