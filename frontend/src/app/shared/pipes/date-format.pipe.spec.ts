import { DateFormatPipe } from './date-format.pipe';

describe('DateFormatPipe', () => {
  const pipe = new DateFormatPipe();

  it('returns an empty string for null', () => {
    expect(pipe.transform(null)).toBe('');
  });

  it('returns an empty string for undefined', () => {
    expect(pipe.transform(undefined)).toBe('');
  });

  it('returns the original value unchanged when it cannot be parsed as a date', () => {
    expect(pipe.transform('not-a-date')).toBe('not-a-date');
  });

  it('formats a date in short form by default (DD Mon YYYY)', () => {
    expect(pipe.transform('2026-03-05T10:00:00')).toBe('05 Mar 2026');
  });

  it('pads single-digit days', () => {
    expect(pipe.transform('2026-03-01T00:00:00')).toBe('01 Mar 2026');
  });

  it('formats a date in long form (DD Mon YYYY, HH:mm) when requested', () => {
    expect(pipe.transform('2026-03-05T14:05:00', 'long')).toBe('05 Mar 2026, 14:05');
  });

  it('pads single-digit hours and minutes in long form', () => {
    expect(pipe.transform('2026-03-05T04:07:00', 'long')).toBe('05 Mar 2026, 04:07');
  });
});
