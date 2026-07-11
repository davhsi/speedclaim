import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { signal } from '@angular/core';
import { NotificationListComponent } from './notification-list';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationDto } from '../../../core/models/api.models';

describe('NotificationListComponent', () => {
  let notifService: {
    notifications: ReturnType<typeof signal<NotificationDto[]>>;
    loadNotifications: ReturnType<typeof vi.fn>;
    markAsRead: ReturnType<typeof vi.fn>;
    markAllAsRead: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  const unread: NotificationDto = {
    id: 'n1', title: 'Claim update', message: 'Your claim moved', isRead: false, createdAt: '2026-01-01',
  } as NotificationDto;
  const read: NotificationDto = {
    id: 'n2', title: 'Payment received', message: 'Thanks', isRead: true, createdAt: '2026-01-02',
  } as NotificationDto;

  function create(list: NotificationDto[] = [unread, read]) {
    notifService.notifications.set(list);
    notifService.loadNotifications.mockReturnValue(of(list));
    TestBed.configureTestingModule({
      imports: [NotificationListComponent],
      providers: [
        { provide: NotificationService, useValue: notifService },
        { provide: Router, useValue: router },
      ],
    });
    const fixture = TestBed.createComponent(NotificationListComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    notifService = {
      notifications: signal<NotificationDto[]>([]),
      loadNotifications: vi.fn(),
      markAsRead: vi.fn(),
      markAllAsRead: vi.fn(),
    };
    router = { navigateByUrl: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads notifications and clears loading', () => {
      const fixture = create();
      expect(notifService.loadNotifications).toHaveBeenCalled();
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('clears loading even when the fetch fails', () => {
      notifService.loadNotifications.mockReturnValue(throwError(() => ({ status: 500 })));
      TestBed.configureTestingModule({
        imports: [NotificationListComponent],
        providers: [
          { provide: NotificationService, useValue: notifService },
          { provide: Router, useValue: router },
        ],
      });
      const fixture = TestBed.createComponent(NotificationListComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('markRead', () => {
    it('does nothing extra for an already-read notification besides navigating', () => {
      const fixture = create();
      fixture.componentInstance.markRead({ ...read, redirectUrl: '/claims/1' });
      expect(notifService.markAsRead).not.toHaveBeenCalled();
      expect(router.navigateByUrl).toHaveBeenCalledWith('/claims/1');
    });

    it('marks isMarkingRead while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const subject = new Subject<any>();
      notifService.markAsRead.mockReturnValue(subject);

      c.markRead(unread);
      expect(c.isMarkingRead('n1')).toBe(true);

      c.markRead(unread);
      expect(notifService.markAsRead).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();

      expect(c.isMarkingRead('n1')).toBe(false);
    });

    it('clears isMarkingRead on error too', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const subject = new Subject<any>();
      notifService.markAsRead.mockReturnValue(subject);

      c.markRead(unread);
      subject.error({ status: 500 });

      expect(c.isMarkingRead('n1')).toBe(false);
    });

    it('navigates immediately even while the mark-as-read call is still in flight', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      notifService.markAsRead.mockReturnValue(new Subject<any>());
      c.markRead({ ...unread, redirectUrl: '/claims/1' });
      expect(router.navigateByUrl).toHaveBeenCalledWith('/claims/1');
    });
  });

  describe('markAllRead', () => {
    it('sets markingAllRead while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const subject = new Subject<any>();
      notifService.markAllAsRead.mockReturnValue(subject);

      c.markAllRead();
      expect(c.markingAllRead()).toBe(true);

      c.markAllRead();
      expect(notifService.markAllAsRead).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();

      expect(c.markingAllRead()).toBe(false);
    });

    it('clears markingAllRead on error too', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const subject = new Subject<any>();
      notifService.markAllAsRead.mockReturnValue(subject);

      c.markAllRead();
      subject.error({ status: 500 });

      expect(c.markingAllRead()).toBe(false);
    });
  });
});
