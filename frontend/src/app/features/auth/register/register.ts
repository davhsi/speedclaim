import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable, Subject } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { passwordStrengthValidator, matchPasswordValidator } from '../../../shared/validators/password.validator';
import { postalCodeValidator, phoneValidator, minAgeValidator } from '../../../shared/validators/input.validators';
import { AppSelectComponent } from '../../../shared/components/app-select/app-select';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { CanComponentDeactivate } from '../../../core/guards/unsaved-changes.guard';

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
  imports: [ReactiveFormsModule, RouterLink, AppSelectComponent, ConfirmDialogComponent],
  templateUrl: './register.html',
  styles: `
    :host { display: block; }
    @keyframes stepFadeIn {
      from { opacity: 0; transform: translateY(8px); }
      to { opacity: 1; transform: translateY(0); }
    }
    .step-animate { animation: stepFadeIn 0.2s ease-out; }
  `,
})
export class RegisterComponent implements CanComponentDeactivate {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  loading = signal(false);
  errorMessage = signal('');
  currentStep = signal(1);
  sameAsPermanent = signal(false);
  showPassword = signal(false);
  showConfirmPassword = signal(false);
  showLeaveConfirm = signal(false);
  private registrationComplete = false;
  private leaveSubject: Subject<boolean> | null = null;

  steps = [
    { label: 'Personal' },
    { label: 'Security' },
    { label: 'Address' },
    { label: 'Consent' },
  ];

  stepDescriptions = [
    "Let's start with your personal details.",
    'Set a password for your account.',
    'Where can we reach you?',
    'Almost done — review and agree.',
  ];

  private readonly stepControls: string[][] = [
    ['salutationTitle', 'firstName', 'lastName', 'email', 'phone', 'dateOfBirth', 'gender', 'maritalStatus'],
    ['password', 'confirmPassword'],
    ['permanentAddress', 'currentAddress'],
    ['consentDataProcessing', 'consentKycCollection'],
  ];

  salutations = ['Mr', 'Mrs', 'Ms', 'Dr'];
  genders = ['Male', 'Female', 'NonBinary', 'Other'];
  maritalStatuses = ['Single', 'Married', 'Divorced', 'Widowed'];
  states = INDIAN_STATES;

  form = this.fb.nonNullable.group({
    salutationTitle: ['', Validators.required],
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, phoneValidator()]],
    dateOfBirth: ['', [Validators.required, minAgeValidator(18)]],
    gender: ['', Validators.required],
    maritalStatus: ['', Validators.required],
    password: ['', [Validators.required, passwordStrengthValidator()]],
    confirmPassword: ['', [Validators.required, matchPasswordValidator('password')]],
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
      this.form.controls.currentAddress.disable();
    } else {
      this.form.controls.currentAddress.enable();
    }
  }

  nextStep(): void {
    const controls = this.stepControls[this.currentStep() - 1];
    controls.forEach(name => {
      const control = this.form.get(name);
      if (control) control.markAllAsTouched();
    });

    if (this.isStepValid(this.currentStep())) {
      this.errorMessage.set('');
      this.currentStep.update(s => s + 1);
    }
  }

  prevStep(): void {
    this.errorMessage.set('');
    this.currentStep.update(s => s - 1);
  }

  isStepValid(step: number): boolean {
    return this.stepControls[step - 1].every(name => {
      const control = this.form.get(name);
      return control ? control.valid || control.disabled : true;
    });
  }

  private getErrorMessage(err: any): string {
    if (err.error?.detail) return err.error.detail;
    if (err.error?.errors && typeof err.error.errors === 'object') {
      const messages = Object.values(err.error.errors)
        .flat()
        .filter((msg): msg is string => typeof msg === 'string');
      if (messages.length > 0) return messages.join(' ');
    }
    if (err.error?.message) return err.error.message;
    if (err.status === 409) return 'This information is already registered.';
    if (err.status === 400) return 'Please check your input and try again.';
    if (err.status >= 500) return 'Something went wrong. Please try again later.';
    return 'Registration failed. Please try again.';
  }

  onSubmit(): void {
    if (this.loading()) return;
    if (this.sameAsPermanent()) {
      const perm = this.form.controls.permanentAddress.getRawValue();
      this.form.controls.currentAddress.patchValue(perm);
    }

    const controls = this.stepControls[this.currentStep() - 1];
    controls.forEach(name => {
      const control = this.form.get(name);
      if (control) control.markAllAsTouched();
    });

    if (!this.isStepValid(this.currentStep())) return;

    this.loading.set(true);
    this.errorMessage.set('');

    const formData = this.form.getRawValue();
    const payload = {
      email: formData.email,
      password: formData.password,
      salutation: formData.salutationTitle,
      firstName: formData.firstName,
      lastName: formData.lastName,
      phone: formData.phone,
      dateOfBirth: formData.dateOfBirth,
      gender: formData.gender,
      maritalStatus: formData.maritalStatus,
      permanentAddress: formData.permanentAddress,
      currentAddress: this.sameAsPermanent() ? formData.permanentAddress : formData.currentAddress,
      isSameAsPermanent: this.sameAsPermanent(),
    };

    this.authService.register(payload as any).subscribe({
      next: () => {
        this.loading.set(false);
        this.registrationComplete = true;
        this.toast.success('Account created! Please check your email to verify.');
        this.router.navigate(['/auth/login']);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(this.getErrorMessage(err));
      },
    });
  }

  canDeactivate(): boolean | Observable<boolean> {
    if (this.registrationComplete || !this.form.dirty) return true;

    this.showLeaveConfirm.set(true);
    this.leaveSubject = new Subject<boolean>();
    return this.leaveSubject.asObservable();
  }

  confirmLeave(): void {
    this.showLeaveConfirm.set(false);
    this.leaveSubject?.next(true);
    this.leaveSubject?.complete();
    this.leaveSubject = null;
  }

  cancelLeave(): void {
    this.showLeaveConfirm.set(false);
    this.leaveSubject?.next(false);
    this.leaveSubject?.complete();
    this.leaveSubject = null;
  }
}
