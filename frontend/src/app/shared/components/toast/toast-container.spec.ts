import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ToastContainerComponent } from './toast-container';
import { ToastService, Toast } from './toast.service';

describe('ToastContainerComponent', () => {
  function create(toasts: Toast[] = []) {
    const toastService = { toasts: signal(toasts) };
    TestBed.configureTestingModule({
      providers: [{ provide: ToastService, useValue: toastService }],
    });
    const fixture = TestBed.createComponent(ToastContainerComponent);
    fixture.detectChanges();
    return { fixture, toastService };
  }

  describe('toastIcon', () => {
    it('returns a distinct icon for each known type', () => {
      const { fixture } = create();
      const c = fixture.componentInstance;
      const icons = new Set([
        c.toastIcon('success'),
        c.toastIcon('error'),
        c.toastIcon('warning'),
        c.toastIcon('info'),
      ]);
      expect(icons.size).toBe(4);
    });

    it('falls back to the success icon for an unknown type', () => {
      const { fixture } = create();
      const c = fixture.componentInstance;
      expect(c.toastIcon('unknown')).toBe(c.toastIcon('success'));
    });
  });

  describe('rendering', () => {
    it('renders nothing when there are no toasts', () => {
      const { fixture } = create([]);
      const alerts = (fixture.nativeElement as HTMLElement).querySelectorAll('[role="alert"]');
      expect(alerts.length).toBe(0);
    });

    it('renders one alert per toast with its message', () => {
      const { fixture } = create([
        { id: 1, message: 'Saved successfully', type: 'success' },
        { id: 2, message: 'Something broke', type: 'error' },
      ]);
      const el = fixture.nativeElement as HTMLElement;
      const alerts = el.querySelectorAll('[role="alert"]');
      expect(alerts.length).toBe(2);
      expect(el.textContent).toContain('Saved successfully');
      expect(el.textContent).toContain('Something broke');
    });

    it('re-renders when the toast list changes', () => {
      const { fixture, toastService } = create([{ id: 1, message: 'First', type: 'info' }]);
      toastService.toasts.set([
        { id: 1, message: 'First', type: 'info' },
        { id: 2, message: 'Second', type: 'warning' },
      ]);
      fixture.detectChanges();

      const alerts = (fixture.nativeElement as HTMLElement).querySelectorAll('[role="alert"]');
      expect(alerts.length).toBe(2);
    });
  });
});
