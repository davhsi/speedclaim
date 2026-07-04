import { Component, inject, signal, computed, OnInit } from '@angular/core';
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
  notifications = this.notifService.notifications;
  loading = signal(true);
  currentPage = signal(1);
  readonly pageSize = 15;

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

  markRead(n: NotificationDto): void {
    if (n.isRead) return;
    this.notifService.markAsRead(n.id).subscribe();
  }

  markAllRead(): void {
    this.notifService.markAllAsRead().subscribe();
  }
}
