import { vi } from 'vitest';
import { FormControl } from '@angular/forms';
import { aadhaarValidator, minAgeValidator, panValidator, phoneValidator, postalCodeValidator } from './input.validators';

describe('aadhaarValidator', () => {
  const validate = aadhaarValidator();

  it('allows an empty value (required is a separate validator)', () => {
    expect(validate(new FormControl(''))).toBeNull();
  });

  it('accepts exactly 12 digits', () => {
    expect(validate(new FormControl('123456789012'))).toBeNull();
  });

  it('rejects fewer than 12 digits', () => {
    expect(validate(new FormControl('12345'))).toEqual({ aadhaar: true });
  });

  it('rejects non-digit characters', () => {
    expect(validate(new FormControl('12345678901a'))).toEqual({ aadhaar: true });
  });
});

describe('panValidator', () => {
  const validate = panValidator();

  it('allows an empty value', () => {
    expect(validate(new FormControl(''))).toBeNull();
  });

  it('accepts a well-formed PAN (5 letters, 4 digits, 1 letter)', () => {
    expect(validate(new FormControl('ABCDE1234F'))).toBeNull();
  });

  it('accepts lowercase input by uppercasing before validating', () => {
    expect(validate(new FormControl('abcde1234f'))).toBeNull();
  });

  it('rejects a malformed PAN', () => {
    expect(validate(new FormControl('12345ABCDE'))).toEqual({ pan: true });
  });
});

describe('postalCodeValidator', () => {
  const validate = postalCodeValidator();

  it('allows an empty value', () => {
    expect(validate(new FormControl(''))).toBeNull();
  });

  it('accepts exactly 6 digits', () => {
    expect(validate(new FormControl('400001'))).toBeNull();
  });

  it('rejects fewer than 6 digits', () => {
    expect(validate(new FormControl('40001'))).toEqual({ postalCode: true });
  });

  it('rejects non-digit characters', () => {
    expect(validate(new FormControl('abcdef'))).toEqual({ postalCode: true });
  });
});

describe('phoneValidator', () => {
  const validate = phoneValidator();

  it('allows an empty value', () => {
    expect(validate(new FormControl(''))).toBeNull();
  });

  it('accepts exactly 10 digits', () => {
    expect(validate(new FormControl('9876543210'))).toBeNull();
  });

  it('rejects fewer than 10 digits', () => {
    expect(validate(new FormControl('987654321'))).toEqual({ phone: true });
  });
});

describe('minAgeValidator', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-07-06T00:00:00'));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('allows an empty value', () => {
    expect(minAgeValidator(18)(new FormControl(''))).toBeNull();
  });

  it('accepts someone whose birthday already fell this year, making them exactly minAge', () => {
    const validate = minAgeValidator(18);
    expect(validate(new FormControl('2008-07-06'))).toBeNull();
  });

  it('rejects someone whose birthday this year has not happened yet, one day short of minAge', () => {
    const validate = minAgeValidator(18);
    expect(validate(new FormControl('2008-07-07'))).toEqual({ minAge: { required: 18, actual: 17 } });
  });

  it('reports the actual age when well under minAge', () => {
    const validate = minAgeValidator(18);
    expect(validate(new FormControl('2010-01-01'))).toEqual({ minAge: { required: 18, actual: 16 } });
  });
});
