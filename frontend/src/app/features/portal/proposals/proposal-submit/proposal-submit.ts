import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProposalService } from '../services/proposal.service';
import { ProfileService } from '../../profile/services/profile.service';
import { FamilyMemberDto, SubmitProposalRequest, DocumentRequirementDto, UserDto } from '../../../../core/models/api.models';
import { FileUploadComponent } from '../../../../shared/components/file-upload/file-upload';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-proposal-submit',
  standalone: true,
  imports: [ReactiveFormsModule, FileUploadComponent],
  templateUrl: './proposal-submit.html',
})
export class ProposalSubmitComponent implements OnInit {
  private fb = inject(FormBuilder);
  private proposalService = inject(ProposalService);
  private profileService = inject(ProfileService);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);

  step = signal(0);
  submitting = signal(false);
  familyMembers = signal<FamilyMemberDto[]>([]);
  docRequirements = signal<DocumentRequirementDto[]>([]);
  profile = signal<UserDto | null>(null);
  uploadedFiles = new Map<string, File>();
  stepLabels = ['Details', 'Nominees', 'Documents'];

  form = this.fb.group({
    productId: ['', Validators.required],
    sumAssured: [0, Validators.required],
    tenureYears: [1, Validators.required],
    premiumAmount: [0, Validators.required],
    paymentFrequency: ['Monthly', Validators.required],
  });

  nominees = this.fb.array([this.createNomineeGroup()]);

  ngOnInit(): void {
    const state = history.state;
    if (state?.productId) {
      this.form.patchValue(state);
      this.loadDocRequirements(state.productId);
    }
    this.profileService.getProfile().subscribe(profile => this.profile.set(profile));
    this.profileService.getFamilyMembers().subscribe(m => this.familyMembers.set(m));
  }

  private createNomineeGroup() {
    return this.fb.group({
      name: ['', Validators.required],
      relationship: ['Spouse', Validators.required],
      sharePercentage: [100, [Validators.required, Validators.min(1), Validators.max(100)]],
      dateOfBirth: ['', Validators.required],
    });
  }

  addNominee(): void { this.nominees.push(this.createNomineeGroup()); }

  onDocSelected(key: string, file: File): void { this.uploadedFiles.set(key, file); }

  private loadDocRequirements(productId: string): void {
    this.http.get<DocumentRequirementDto[]>(`/api/v1/products/${productId}/documents`)
      .subscribe(docs => this.docRequirements.set(docs));
  }

  submit(): void {
    const customerId = this.profile()?.customerId;
    if (!customerId) {
      this.toast.error('Customer profile is not ready yet');
      return;
    }

    this.submitting.set(true);
    const formValue = this.form.getRawValue();
    const req: SubmitProposalRequest = {
      customerId,
      productId: formValue.productId!,
      sumAssured: formValue.sumAssured!,
      tenureYears: formValue.tenureYears!,
      premiumAmount: formValue.premiumAmount!,
      paymentFrequency: formValue.paymentFrequency as any,
      customerMemberIds: [],
      nominees: this.nominees.getRawValue().map(n => ({
        fullName: n.name!,
        relationship: n.relationship as any,
        sharePercentage: n.sharePercentage!,
        dateOfBirth: n.dateOfBirth!,
        isMinor: this.isMinor(n.dateOfBirth!),
      })),
    };
    this.proposalService.submit(req).subscribe({
      next: proposal => {
        const uploads = Array.from(this.uploadedFiles.entries());
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
}
