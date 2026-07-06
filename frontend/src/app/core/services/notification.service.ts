import { Injectable, inject, signal, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { NotificationDto, ApiMessage } from '../models/api.models';
import { TokenService } from './token.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly tokenService = inject(TokenService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly apiUrl = '/api/v1/users/notifications';

  private hubConnection: signalR.HubConnection | null = null;

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

  markAsRead(id: string): Observable<ApiMessage> {
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

  startRealtime(): void {
    if (!isPlatformBrowser(this.platformId) || this.hubConnection) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => this.tokenService.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
      this.notifications.update(list => [notification, ...list]);
      this.unreadCount.update(c => c + 1);
    });

    this.hubConnection.start().catch(err => console.error('SignalR connection failed:', err));
  }

  stopRealtime(): void {
    this.hubConnection?.stop();
    this.hubConnection = null;
  }
}
