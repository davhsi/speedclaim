import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { passwordStrengthValidator, matchPasswordValidator } from '../../../shared/validators/password.validator';
import { aadhaarValidator, panValidator, postalCodeValidator, phoneValidator, minAgeValidator } from '../../../shared/validators/input.validators';

const INDIAN_STATES = [
  'Andhra Pradesh', 'Arunachal Pradesh', 'Assam', 'Bihar', 'Chhattisgarh', 'Goa', 'Gujarat',
  'Haryana', 'Himachal Pradesh', 'Jharkhand', 'Karnataka', 'Kerala', 'Madhya Pradesh',
  'Maharashtra', 'Manipur', 'Meghalaya', 'Mizoram', 'Nagaland', 'Odisha', 'Punjab',
  'Rajasthan', 'Sikkim', 'Tamil Nadu', 'Telangana', 'Tripura', 'Uttar Pradesh',
  'Uttarakhand', 'West Bengal', 'Delhi', 'Jammu and Kashmir', 'Ladakh',
  'Andaman and Nicobar Islands', 'Chandigarh', 'Dadra and Nagar Haveli and Daman and Diu',
  'Lakshadweep', 'Puducherry',
];

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styles: `
    :host { display: block; max-height: 70vh; overflow-y: auto; }
    fieldset { border: none; padding: 0; margin: 0; }
  `,
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  loading = signal(false);
  errorMessage = signal('');
  sameAsPermanent = signal(false);

  salutations = ['Mr', 'Mrs', 'Ms', 'Dr'];
  genders = ['Male', 'Female', 'NonBinary', 'Other'];
  maritalStatuses = ['Single', 'Married', 'Divorced', 'Widowed'];
  states = INDIAN_STATES;

  form = this.fb.nonNullable.group({
    salutationTitle: ['', Validators.required],
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, phoneValidator()]],
    dateOfBirth: ['', [Validators.required, minAgeValidator(18)]],
    gender: ['', Validators.required],
    maritalStatus: ['', Validators.required],
    password: ['', [Validators.required, passwordStrengthValidator()]],
    confirmPassword: ['', [Validators.required, matchPasswordValidator('password')]],
    aadhaarNumber: ['', [Validators.required, aadhaarValidator()]],
    panNumber: ['', [Validators.required, panValidator()]],
    permanentAddress: this.fb.nonNullable.group({
      line1: ['', Validators.required],
      line2: [''],
      city: ['', Validators.required],
      state: ['', Validators.required],
      postalCode: ['', [Validators.required, postalCodeValidator()]],
      country: ['India'],
    }),
    currentAddress: this.fb.nonNullable.group({
      line1: ['', Validators.required],
      line2: [''],
      city: ['', Validators.required],
      state: ['', Validators.required],
      postalCode: ['', [Validators.required, postalCodeValidator()]],
      country: ['India'],
    }),
    consentDataProcessing: [false, Validators.requiredTrue],
    consentKycCollection: [false, Validators.requiredTrue],
  });

  toggleSameAddress(): void {
    this.sameAsPermanent.update(v => !v);
    if (this.sameAsPermanent()) {
      const perm = this.form.controls.permanentAddress.getRawValue();
      this.form.controls.currentAddress.patchValue(perm);
    }
  }

  onSubmit(): void {
    if (this.sameAsPermanent()) {
      const perm = this.form.controls.permanentAddress.getRawValue();
      this.form.controls.currentAddress.patchValue(perm);
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Please fix the errors above before submitting.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    const { confirmPassword, ...payload } = this.form.getRawValue();
    this.authService.register(payload as any).subscribe({
      next: () => {
        this.loading.set(false);
        this.toast.success('Account created! Please check your email to verify.');
        this.router.navigate(['/auth/login']);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 409) {
          this.errorMessage.set('An account with this email already exists.');
        } else if (err.error?.errors) {
          const messages = Object.values(err.error.errors).flat().join(' ');
          this.errorMessage.set(messages);
        } else {
          this.errorMessage.set('Registration failed. Please try again.');
        }
      },
    });
  }
}
