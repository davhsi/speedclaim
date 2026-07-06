import { MoneyPipe } from './money.pipe';

describe('MoneyPipe', () => {
  const pipe = new MoneyPipe();

  it('defaults null to ₹ 0.00', () => {
    expect(pipe.transform(null)).toBe('₹ 0.00');
  });

  it('defaults undefined to ₹ 0.00', () => {
    expect(pipe.transform(undefined)).toBe('₹ 0.00');
  });

  it('formats zero', () => {
    expect(pipe.transform(0)).toBe('₹ 0.00');
  });

  it('formats a sub-thousand amount with no grouping separator', () => {
    expect(pipe.transform(100)).toBe('₹ 100.00');
  });

  it('applies Indian digit grouping (3, then 2s) above a thousand', () => {
    expect(pipe.transform(1234.5)).toBe('₹ 1,234.50');
  });

  it('applies Indian digit grouping for large amounts (lakhs/crores)', () => {
    expect(pipe.transform(12345678)).toBe('₹ 1,23,45,678.00');
  });

  it('prefixes negative amounts with a minus sign before the symbol', () => {
    expect(pipe.transform(-500)).toBe('-₹ 500.00');
  });

  it('accepts a custom currency symbol', () => {
    expect(pipe.transform(100, '$')).toBe('$ 100.00');
  });
});
