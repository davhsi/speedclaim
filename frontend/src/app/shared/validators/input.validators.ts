import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function aadhaarValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    return /^\d{12}$/.test(value) ? null : { aadhaar: true };
  };
}

export function panValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    return /^[A-Z]{5}\d{4}[A-Z]$/.test(value.toUpperCase()) ? null : { pan: true };
  };
}

export function postalCodeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    return /^\d{6}$/.test(value) ? null : { postalCode: true };
  };
}

export function phoneValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    return /^\d{10}$/.test(value) ? null : { phone: true };
  };
}

export function minAgeValidator(minAge: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;
    const dob = new Date(value);
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const m = today.getMonth() - dob.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < dob.getDate())) age--;
    return age < minAge ? { minAge: { required: minAge, actual: age } } : null;
  };
}
