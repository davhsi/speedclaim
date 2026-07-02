import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-surveyor-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: './surveyor-layout.html',
  styles: `
    @keyframes slide-up {
      from { transform: translateY(100%); }
      to { transform: translateY(0); }
    }
    .animate-slide-up { animation: slide-up 0.25s ease-out; }
  `,
})
export class SurveyorLayoutComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  notifService = inject(NotificationService);

  activeTab = signal<'claims' | 'history' | 'profile'>('claims');
  isReportRoute = signal(false);
  showNotifs = signal(false);

  ngOnInit(): void {
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
    ).subscribe(e => this.resolveTab(e.urlAfterRedirects));
    this.resolveTab(this.router.url);
    this.notifService.loadNotifications().subscribe();
  }

  private resolveTab(url: string): void {
    this.isReportRoute.set(url.includes('/report'));
    if (url.includes('/history')) this.activeTab.set('history');
    else if (url.includes('/profile')) this.activeTab.set('profile');
    else this.activeTab.set('claims');
  }

  openNotifications(): void {
    this.showNotifs.set(true);
  }

  markAllRead(): void {
    this.notifService.markAllAsRead().subscribe();
  }

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  notifIconBg(type: string): string {
    const m: Record<string, string> = { Warning: 'bg-warning-bg', Success: 'bg-success-bg', Error: 'bg-danger-bg' };
    return m[type] ?? 'bg-primary-light';
  }

  notifIconColor(type: string): string {
    const m: Record<string, string> = { Warning: '#D9920A', Success: '#1F9D6B', Error: '#D14343' };
    return m[type] ?? '#091520';
  }
}
