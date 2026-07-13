import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Observable, Subject } from 'rxjs';
import { ProposalService } from '../services/proposal.service';
import { ProfileService } from '../../profile/services/profile.service';
import { FamilyMemberDto, SubmitProposalRequest, DocumentRequirementDto, UserDto } from '../../../../core/models/api.models';
import { FileUploadComponent } from '../../../../shared/components/file-upload/file-upload';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { CanComponentDeactivate } from '../../../../core/guards/unsaved-changes.guard';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-proposal-submit',
  standalone: true,
  imports: [ReactiveFormsModule, FileUploadComponent, ConfirmDialogComponent],
  templateUrl: './proposal-submit.html',
})
export class ProposalSubmitComponent implements OnInit, CanComponentDeactivate {
  private readonly fb = inject(FormBuilder);
  private readonly proposalService = inject(ProposalService);
  private readonly profileService = inject(ProfileService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);

  step = signal(0);
  submitting = signal(false);
  familyMembers = signal<FamilyMemberDto[]>([]);
  docRequirements = signal<DocumentRequirementDto[]>([]);
  docRequirementsLoaded = signal(false);
  profile = signal<UserDto | null>(null);
  uploadedFiles = new Map<string, File>();
  stepLabels = ['Details', 'Nominees', 'Documents'];
  readonly relationshipOptions = ['Spouse', 'Husband', 'Wife', 'Son', 'Daughter', 'Father', 'Mother', 'Brother', 'Sister', 'Guardian', 'Other'];
  domain = signal<string | null>(null);
  quoteMotorDetail = signal<{ vehicleMake?: string; vehicleModel?: string; manufactureYear?: number; insuredDeclaredValue?: number } | null>(null);
  productMotorVehicleType = signal<string | null>(null);

  form = this.fb.group({
    productId: ['', Validators.required],
    sumAssured: [0, Validators.required],
    tenureYears: [1, Validators.required],
    premiumAmount: [0, Validators.required],
    paymentFrequency: ['Monthly', Validators.required],
  });

  nominees = this.fb.array([this.createNomineeGroup()]);

  motorForm = this.fb.group({
    vehicleNumber: ['', Validators.required],
    vehicleMake: ['', Validators.required],
    vehicleModel: ['', Validators.required],
    manufactureYear: [new Date().getFullYear(), [Validators.required, Validators.min(1980)]],
    engineNumber: ['', Validators.required],
    chassisNumber: ['', Validators.required],
    motorVehicleType: ['', Validators.required],
    coverType: ['Comprehensive', Validators.required],
  });

  isMotor(): boolean {
    return (this.domain() ?? '').toUpperCase() === 'MOTOR';
  }

  isLife(): boolean {
    return (this.domain() ?? '').toUpperCase() === 'LIFE';
  }

  showLeaveConfirm = signal(false);
  private navigatedAfterSubmit = false;
  private leaveSubject: Subject<boolean> | null = null;

  canDeactivate(): boolean | Observable<boolean> {
    const dirty = this.form.dirty || this.nominees.dirty || this.motorForm.dirty || this.uploadedFiles.size > 0;
    if (this.navigatedAfterSubmit || !dirty) return true;

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

  ngOnInit(): void {
    const state = history.state;
    if (state?.productId) {
      this.form.patchValue(state);
      this.domain.set(state.domain ?? null);
      this.productMotorVehicleType.set(state.motorVehicleType ?? null);
      if (state.motorDetail) {
        this.quoteMotorDetail.set(state.motorDetail);
        this.motorForm.patchValue({
          vehicleMake: state.motorDetail.vehicleMake ?? '',
          vehicleModel: state.motorDetail.vehicleModel ?? '',
          manufactureYear: state.motorDetail.manufactureYear ?? new Date().getFullYear(),
          motorVehicleType: state.motorVehicleType ?? '',
        });
        this.motorForm.controls.vehicleMake.disable();
        this.motorForm.controls.vehicleModel.disable();
        this.motorForm.controls.manufactureYear.disable();
      }
      if (state.motorVehicleType) {
        this.motorForm.controls.motorVehicleType.disable();
      }
      this.form.controls.productId.disable();
      this.form.controls.sumAssured.disable();
      this.form.controls.tenureYears.disable();
      this.form.controls.premiumAmount.disable();
      this.form.controls.paymentFrequency.disable();
      this.loadDocRequirements(state.productId);
    } else {
      this.toast.warning('Please generate a quote before submitting a proposal.');
      this.router.navigate(['/quote'], { replaceUrl: true });
      return;
    }
    this.profileService.getProfile().subscribe(profile => this.profile.set(profile));
    this.profileService.getFamilyMembers().subscribe(m => this.familyMembers.set(m));
  }

  private createNomineeGroup() {
    return this.fb.group({
      name: ['', Validators.required],
      relationship: ['Spouse', Validators.required],
      sharePercentage: [100, [Validators.required, Validators.min(1), Validators.max(100)]],
      dateOfBirth: ['', [Validators.required, this.pastDateValidator]],
      appointeeName: [''],
    });
  }

  addNominee(): void { this.nominees.push(this.createNomineeGroup()); }

  isMinorNominee(index: number): boolean {
    return this.isMinor(this.nominees.at(index).get('dateOfBirth')?.value ?? '');
  }

  get totalShares(): number {
    return this.nominees.controls.reduce((sum, g) => sum + (Number(g.get('sharePercentage')?.value) || 0), 0);
  }

  get nomineesValid(): boolean {
    if (!this.isLife()) return true;
    if (!this.nominees.valid || this.nominees.length === 0 || this.totalShares !== 100) return false;
    return this.nominees.controls.every((_, i) =>
      !this.isMinorNominee(i) || !!this.nominees.at(i).get('appointeeName')?.value?.trim()
    );
  }

  continueFromDetails(): void {
    this.step.set(this.isLife() ? 1 : 2);
  }

  backFromDocuments(): void {
    this.step.set(this.isLife() ? 1 : 0);
  }

  onDocSelected(key: string, file: File): void { this.uploadedFiles.set(key, file); }

  requiredDocumentsUploaded(): boolean {
    return this.docRequirementsLoaded() && this.docRequirements()
      .filter(d => d.isMandatory)
      .every(d => this.uploadedFiles.has(d.documentKey));
  }

  private loadDocRequirements(productId: string): void {
    this.http.get<DocumentRequirementDto[]>(`/api/v1/products/${productId}/documents`)
      .subscribe({
        next: docs => {
          this.docRequirements.set(docs);
          this.docRequirementsLoaded.set(true);
        },
        error: () => {
          this.docRequirementsLoaded.set(true);
          this.toast.warning('Document requirements could not be loaded.');
        },
      });
  }

  submit(): void {
    if (this.submitting()) return;
    const customerId = this.profile()?.customerId;
    if (!customerId) {
      this.toast.error('Customer profile is not ready yet');
      return;
    }
    if (this.form.invalid || !this.nomineesValid) {
      this.toast.warning('Please complete all required fields before submitting.');
      return;
    }
    if (this.isMotor() && this.motorForm.invalid) {
      this.motorForm.markAllAsTouched();
      this.toast.warning('Please complete the vehicle details before submitting.');
      return;
    }
    if (!this.requiredDocumentsUploaded()) {
      this.toast.warning('Please upload all required documents before submitting.');
      return;
    }

    this.submitting.set(true);
    const formValue = this.form.getRawValue();
    const motorValue = this.motorForm.getRawValue();
    const req: SubmitProposalRequest = {
      customerId,
      productId: formValue.productId!,
      sumAssured: formValue.sumAssured!,
      tenureYears: formValue.tenureYears!,
      premiumAmount: formValue.premiumAmount!,
      paymentFrequency: formValue.paymentFrequency as any,
      motorDetail: this.isMotor() ? {
        vehicleNumber: motorValue.vehicleNumber!.trim(),
        vehicleMake: motorValue.vehicleMake!.trim(),
        vehicleModel: motorValue.vehicleModel!.trim(),
        manufactureYear: Number(motorValue.manufactureYear),
        vehicleType: motorValue.motorVehicleType!,
        idv: this.quoteMotorDetail()?.insuredDeclaredValue ?? formValue.sumAssured!,
        engineNumber: motorValue.engineNumber!.trim(),
        chassisNumber: motorValue.chassisNumber!.trim(),
        coverType: motorValue.coverType!,
      } : undefined,
      customerMemberIds: [],
      nominees: this.isLife() ? this.nominees.getRawValue().map(n => ({
        fullName: n.name!,
        relationship: n.relationship as any,
        sharePercentage: n.sharePercentage!,
        dateOfBirth: n.dateOfBirth!,
        isMinor: this.isMinor(n.dateOfBirth!),
        appointeeName: n.appointeeName?.trim() || undefined,
      })) : [],
    };
    this.proposalService.submit(req).subscribe({
      next: proposal => {
        const uploads = Array.from(this.uploadedFiles.entries());
        this.navigatedAfterSubmit = true;
        if (uploads.length === 0) {
          this.toast.success('Proposal submitted');
          this.router.navigate(['/proposals', proposal.id]);
          return;
        }
        let done = 0;
        for (const [key, file] of uploads) {
          this.proposalService.uploadDocument(proposal.id, key, file).subscribe({
            next: () => { if (++done === uploads.length) { this.toast.success('Proposal submitted'); this.router.navigate(['/proposals', proposal.id]); } },
            error: () => { if (++done === uploads.length) { this.toast.warning('Proposal submitted but some documents failed'); this.router.navigate(['/proposals', proposal.id]); } },
          });
        }
      },
      error: () => { this.submitting.set(false); this.toast.error('Submission failed'); },
    });
  }

  private isMinor(dateOfBirth: string): boolean {
    const dob = new Date(dateOfBirth);
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const monthDelta = today.getMonth() - dob.getMonth();
    if (monthDelta < 0 || (monthDelta === 0 && today.getDate() < dob.getDate())) {
      age--;
    }
    return age < 18;
  }

  private pastDateValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) return null;
    const selected = new Date(`${value}T00:00:00`);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return selected < today ? null : { pastDate: true };
  }
}
