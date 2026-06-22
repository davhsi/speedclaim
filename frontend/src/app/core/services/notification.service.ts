import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { NotificationDto, ApiMessage } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/users/notifications';

  notifications = signal<NotificationDto[]>([]);
  unreadCount = signal<number>(0);

  loadNotifications(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>(this.apiUrl).pipe(
      tap(list => {
        this.notifications.set(list);
        this.unreadCount.set(list.filter(n => !n.isRead).length);
      }),
    );
  }

  markAsRead(id: number): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`${this.apiUrl}/${id}/read`, {}).pipe(
      tap(() => {
        this.notifications.update(list =>
          list.map(n => n.id === id ? { ...n, isRead: true } : n),
        );
        this.unreadCount.update(c => Math.max(0, c - 1));
      }),
    );
  }

  markAllAsRead(): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`${this.apiUrl}/read-all`, {}).pipe(
      tap(() => {
        this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
        this.unreadCount.set(0);
      }),
    );
  }
}
