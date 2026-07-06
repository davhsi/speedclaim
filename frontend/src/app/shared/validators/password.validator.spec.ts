import { FormControl, FormGroup } from '@angular/forms';
import { matchPasswordValidator, passwordStrengthValidator } from './password.validator';

describe('passwordStrengthValidator', () => {
  const validate = passwordStrengthValidator();

  it('allows an empty value (required is a separate validator)', () => {
    expect(validate(new FormControl(''))).toBeNull();
  });

  it('accepts a password meeting every rule', () => {
    expect(validate(new FormControl('Abcdef1!'))).toBeNull();
  });

  it('flags a password shorter than 8 characters', () => {
    const result = validate(new FormControl('Ab1!'));
    expect(result?.['passwordStrength']).toEqual(
      expect.objectContaining({ minLength: true }),
    );
  });

  it('flags a missing uppercase letter', () => {
    const result = validate(new FormControl('abcdefg1!'));
    expect(result?.['passwordStrength']).toEqual({ uppercase: true });
  });

  it('flags a missing lowercase letter', () => {
    const result = validate(new FormControl('ABCDEFG1!'));
    expect(result?.['passwordStrength']).toEqual({ lowercase: true });
  });

  it('flags a missing digit', () => {
    const result = validate(new FormControl('Abcdefgh!'));
    expect(result?.['passwordStrength']).toEqual({ digit: true });
  });

  it('flags a missing special character', () => {
    const result = validate(new FormControl('Abcdefg1'));
    expect(result?.['passwordStrength']).toEqual({ special: true });
  });

  it('can flag multiple missing rules at once', () => {
    const result = validate(new FormControl('abcdefgh'));
    expect(result?.['passwordStrength']).toEqual({ uppercase: true, digit: true, special: true });
  });
});

describe('matchPasswordValidator', () => {
  // matchPasswordValidator reads control.parent, which Angular only wires up once the
  // control is registered on the FormGroup — i.e. after the control's own constructor
  // (and its first validation pass) has already run. So each control's validator must be
  // re-run once via updateValueAndValidity() after the group exists, matching what happens
  // naturally the moment a real user interacts with the form.
  it('returns null when the confirmation matches the sibling password field', () => {
    const group = new FormGroup({
      password: new FormControl('Secret123!'),
      confirmPassword: new FormControl('Secret123!', matchPasswordValidator('password')),
    });
    group.get('confirmPassword')?.updateValueAndValidity();

    expect(group.get('confirmPassword')?.errors).toBeNull();
  });

  it('returns passwordMismatch when the confirmation differs from the sibling password field', () => {
    const group = new FormGroup({
      password: new FormControl('Secret123!'),
      confirmPassword: new FormControl('Different1!', matchPasswordValidator('password')),
    });
    group.get('confirmPassword')?.updateValueAndValidity();

    expect(group.get('confirmPassword')?.errors).toEqual({ passwordMismatch: true });
  });

  it('re-validates when the sibling password field changes', () => {
    const group = new FormGroup({
      password: new FormControl('Secret123!'),
      confirmPassword: new FormControl('Secret123!', matchPasswordValidator('password')),
    });

    group.get('password')?.setValue('Changed456!');
    group.get('confirmPassword')?.updateValueAndValidity();

    expect(group.get('confirmPassword')?.errors).toEqual({ passwordMismatch: true });
  });
});
