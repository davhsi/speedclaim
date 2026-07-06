import { vi } from 'vitest';
import { TimeAgoPipe } from './time-ago.pipe';

describe('TimeAgoPipe', () => {
  const pipe = new TimeAgoPipe();
  const now = new Date('2026-07-06T12:00:00');

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(now);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('formats a moment less than a minute ago as "0m ago"', () => {
    const thirtySecondsAgo = new Date(now.getTime() - 30_000).toISOString();
    expect(pipe.transform(thirtySecondsAgo)).toBe('0m ago');
  });

  it('formats minutes ago', () => {
    const fiveMinAgo = new Date(now.getTime() - 5 * 60_000).toISOString();
    expect(pipe.transform(fiveMinAgo)).toBe('5m ago');
  });

  it('stays in minutes just under an hour', () => {
    const fiftyNineMinAgo = new Date(now.getTime() - 59 * 60_000).toISOString();
    expect(pipe.transform(fiftyNineMinAgo)).toBe('59m ago');
  });

  it('switches to hours at exactly 60 minutes', () => {
    const oneHourAgo = new Date(now.getTime() - 60 * 60_000).toISOString();
    expect(pipe.transform(oneHourAgo)).toBe('1h ago');
  });

  it('stays in hours just under a day', () => {
    const twentyThreeHoursAgo = new Date(now.getTime() - 23 * 3_600_000).toISOString();
    expect(pipe.transform(twentyThreeHoursAgo)).toBe('23h ago');
  });

  it('falls back to a formatted date at exactly 24 hours', () => {
    const oneDayAgo = new Date(now.getTime() - 24 * 3_600_000);
    const expected = oneDayAgo.toLocaleDateString('en-IN', { day: 'numeric', month: 'short' });
    expect(pipe.transform(oneDayAgo.toISOString())).toBe(expected);
  });
});
