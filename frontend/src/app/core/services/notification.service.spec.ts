import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import * as signalR from '@microsoft/signalr';
import { NotificationService } from './notification.service';
import { TokenService } from './token.service';
import { NotificationDto } from '../models/api.models';

// The real SignalR HubConnectionBuilder resolves relative URLs against `window.location`
// in a way that throws in this test environment, so it's mocked out here rather than
// exercised — startRealtime/stopRealtime are tested against the guard logic and the
// 'ReceiveNotification' handler wiring, not real transport behavior.
const { mockConnection, HubConnectionBuilderMock } = vi.hoisted(() => {
  const mockConnection = {
    on: vi.fn(),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
  };
  const mockBuilder = {
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    build: vi.fn(() => mockConnection),
  };
  const HubConnectionBuilderMock = vi.fn(function (this: unknown) {
    return mockBuilder;
  });
  return { mockConnection, HubConnectionBuilderMock };
});

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;

  const notifications: NotificationDto[] = [
    { id: 'n1', isRead: false } as NotificationDto,
    { id: 'n2', isRead: true } as NotificationDto,
    { id: 'n3', isRead: false } as NotificationDto,
  ];

  beforeEach(() => {
    HubConnectionBuilderMock.mockClear();
    mockConnection.on.mockClear();
    mockConnection.start.mockClear();
    mockConnection.stop.mockClear();
    vi.spyOn(signalR, 'HubConnectionBuilder').mockImplementation(HubConnectionBuilderMock as unknown as typeof signalR.HubConnectionBuilder);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: TokenService, useValue: { getAccessToken: () => 'access-1' } },
      ],
    });

    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.restoreAllMocks();
  });

  describe('loadNotifications', () => {
    it('fetches the list and derives unreadCount from isRead flags', () => {
      service.loadNotifications().subscribe(res => {
        expect(res).toEqual(notifications);
      });

      const call = httpMock.expectOne('/api/v1/users/notifications');
      expect(call.request.method).toBe('GET');
      call.flush(notifications);

      expect(service.notifications()).toEqual(notifications);
      expect(service.unreadCount()).toBe(2);
    });

    it('sets unreadCount to 0 when the list is empty', () => {
      service.loadNotifications().subscribe();
      const call = httpMock.expectOne('/api/v1/users/notifications');
      call.flush([]);
      expect(service.unreadCount()).toBe(0);
    });
  });

  describe('markAsRead', () => {
    it('patches the given id, flips it to read locally, and decrements unreadCount', () => {
      service.notifications.set(notifications);
      service.unreadCount.set(2);

      service.markAsRead('n1').subscribe();

      const call = httpMock.expectOne('/api/v1/users/notifications/n1/read');
      expect(call.request.method).toBe('PATCH');
      call.flush({ message: 'ok' });

      expect(service.notifications().find(n => n.id === 'n1')?.isRead).toBe(true);
      expect(service.unreadCount()).toBe(1);
    });

    it('never lets unreadCount go below 0', () => {
      service.notifications.set(notifications);
      service.unreadCount.set(0);

      service.markAsRead('n1').subscribe();
      const call = httpMock.expectOne('/api/v1/users/notifications/n1/read');
      call.flush({ message: 'ok' });

      expect(service.unreadCount()).toBe(0);
    });
  });

  describe('markAllAsRead', () => {
    it('patches read-all, marks every notification read, and zeroes unreadCount', () => {
      service.notifications.set(notifications);
      service.unreadCount.set(2);

      service.markAllAsRead().subscribe();

      const call = httpMock.expectOne('/api/v1/users/notifications/read-all');
      expect(call.request.method).toBe('PATCH');
      call.flush({ message: 'ok' });

      expect(service.notifications().every(n => n.isRead)).toBe(true);
      expect(service.unreadCount()).toBe(0);
    });
  });

  describe('startRealtime / stopRealtime', () => {
    it('is safe to call stopRealtime when no connection was ever started', () => {
      expect(() => service.stopRealtime()).not.toThrow();
    });

    it('builds a single hub connection and starts it', () => {
      service.startRealtime();

      expect(HubConnectionBuilderMock).toHaveBeenCalledTimes(1);
      expect(mockConnection.on).toHaveBeenCalledWith('ReceiveNotification', expect.any(Function));
      expect(mockConnection.start).toHaveBeenCalledTimes(1);
    });

    it('does not build a second connection when already started', () => {
      service.startRealtime();
      service.startRealtime();

      expect(HubConnectionBuilderMock).toHaveBeenCalledTimes(1);
    });

    it('prepends an incoming ReceiveNotification push and increments unreadCount', () => {
      service.notifications.set([notifications[1]]);
      service.unreadCount.set(0);

      service.startRealtime();
      const receiveHandler = mockConnection.on.mock.calls.find(([event]) => event === 'ReceiveNotification')?.[1];
      const pushed: NotificationDto = { id: 'n4', isRead: false } as NotificationDto;

      receiveHandler(pushed);

      expect(service.notifications()).toEqual([pushed, notifications[1]]);
      expect(service.unreadCount()).toBe(1);
    });

    it('stops and clears the connection so a later startRealtime rebuilds it', () => {
      service.startRealtime();
      service.stopRealtime();

      expect(mockConnection.stop).toHaveBeenCalledTimes(1);

      service.startRealtime();
      expect(HubConnectionBuilderMock).toHaveBeenCalledTimes(2);
    });
  });
});
