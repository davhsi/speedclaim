import { vi } from 'vitest';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    vi.useFakeTimers();
    service = new ToastService();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('adds a toast of each type with the right message', () => {
    service.success('saved');
    service.error('failed');
    service.warning('careful');
    service.info('fyi');

    const types = service.toasts().map(t => t.type);
    const messages = service.toasts().map(t => t.message);
    expect(types).toEqual(['success', 'error', 'warning', 'info']);
    expect(messages).toEqual(['saved', 'failed', 'careful', 'fyi']);
  });

  it('assigns sequential ids starting at 0', () => {
    service.success('first');
    service.success('second');
    expect(service.toasts().map(t => t.id)).toEqual([0, 1]);
  });

  it('removes a toast by id', () => {
    service.success('first');
    service.success('second');
    const firstId = service.toasts()[0].id;

    service.remove(firstId);

    expect(service.toasts().map(t => t.message)).toEqual(['second']);
  });

  it('auto-dismisses a toast after 5 seconds', () => {
    service.success('will vanish');
    expect(service.toasts()).toHaveLength(1);

    vi.advanceTimersByTime(5000);

    expect(service.toasts()).toHaveLength(0);
  });

  it('does not remove other toasts when one auto-dismisses', () => {
    service.success('first');
    vi.advanceTimersByTime(2000);
    service.success('second');

    vi.advanceTimersByTime(3000); // first now at 5s, second at 3s
    expect(service.toasts().map(t => t.message)).toEqual(['second']);

    vi.advanceTimersByTime(2000); // second now at 5s
    expect(service.toasts()).toHaveLength(0);
  });
});
