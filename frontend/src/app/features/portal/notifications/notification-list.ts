import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationDto } from '../../../core/models/api.models';
import { NotificationService } from '../../../core/services/notification.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [EmptyStateComponent, DateFormatPipe, PaginationComponent],
  templateUrl: './notification-list.html',
})
export class NotificationListComponent implements OnInit {
  private readonly notifService = inject(NotificationService);
  private readonly router = inject(Router);
  notifications = this.notifService.notifications;
  loading = signal(true);
  currentPage = signal(1);
  readonly pageSize = 15;
  markingReadIds = signal<Set<string>>(new Set());
  markingAllRead = signal(false);

  totalPages = computed(() => Math.max(1, Math.ceil(this.notifications().length / this.pageSize)));
  pagedNotifications = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.notifications().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }

  ngOnInit(): void {
    this.notifService.loadNotifications().subscribe({
      next: () => this.loading.set(false),
      error: () => this.loading.set(false),
    });
  }

  isMarkingRead(id: string): boolean {
    return this.markingReadIds().has(id);
  }

  markRead(n: NotificationDto): void {
    if (!n.isRead && !this.isMarkingRead(n.id)) {
      this.markingReadIds.update(ids => new Set(ids).add(n.id));
      this.notifService.markAsRead(n.id).subscribe({
        next: () => this.markingReadIds.update(ids => { const s = new Set(ids); s.delete(n.id); return s; }),
        error: () => this.markingReadIds.update(ids => { const s = new Set(ids); s.delete(n.id); return s; }),
      });
    }
    if (n.redirectUrl) {
      this.router.navigateByUrl(n.redirectUrl);
    }
  }

  markAllRead(): void {
    if (this.markingAllRead()) return;
    this.markingAllRead.set(true);
    this.notifService.markAllAsRead().subscribe({
      next: () => this.markingAllRead.set(false),
      error: () => this.markingAllRead.set(false),
    });
  }
}
