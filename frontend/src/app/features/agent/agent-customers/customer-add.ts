import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AgentService } from '../services/agent.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { postalCodeValidator, phoneValidator, minAgeValidator } from '../../../shared/validators/input.validators';
import { AppSelectComponent } from '../../../shared/components/app-select/app-select';

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
  selector: 'app-agent-customer-add',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, AppSelectComponent],
  templateUrl: './customer-add.html',
})
export class AgentCustomerAddComponent {
  private readonly fb = inject(FormBuilder);
  private readonly agentService = inject(AgentService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

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
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, phoneValidator()]],
    dateOfBirth: ['', [Validators.required, minAgeValidator(18)]],
    gender: ['', Validators.required],
    maritalStatus: ['', Validators.required],
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
    return 'Failed to add customer. Please try again.';
  }

  onSubmit(): void {
    if (this.loading()) return;
    if (this.sameAsPermanent()) {
      const perm = this.form.controls.permanentAddress.getRawValue();
      this.form.controls.currentAddress.patchValue(perm);
    }

    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading.set(true);
    this.errorMessage.set('');

    const formData = this.form.getRawValue();
    const payload = {
      email: formData.email,
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

    this.agentService.addCustomer(payload as any).subscribe({
      next: () => {
        this.loading.set(false);
        this.toast.success('Customer added — they’ll get an email to set their password');
        this.router.navigate(['/agent/customers']);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(this.getErrorMessage(err));
      },
    });
  }
}
