import { Component, inject, signal, OnInit } from '@angular/core';
import { NotificationDto } from '../../../core/models/api.models';
import { NotificationService } from '../../../core/services/notification.service';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [EmptyStateComponent, DateFormatPipe],
  templateUrl: './notification-list.html',
})
export class NotificationListComponent implements OnInit {
  private notifService = inject(NotificationService);
  notifications = this.notifService.notifications;
  loading = signal(true);

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
