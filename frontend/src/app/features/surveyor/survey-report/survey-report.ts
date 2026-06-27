import { Component, inject, signal, computed, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SurveyorService } from '../services/surveyor.service';
import { ClaimDto } from '../../../core/models/api.models';
import { ToastService } from '../../../shared/components/toast/toast.service';

type DamageType = '' | 'Partial loss' | 'Total loss' | 'Third party' | 'Theft';

@Component({
  selector: 'app-survey-report',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './survey-report.html',
})
export class SurveyReportComponent implements OnInit {
  @ViewChild('photoInput') photoInput!: ElementRef<HTMLInputElement>;
  @ViewChild('docInput') docInput!: ElementRef<HTMLInputElement>;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private surveyorService = inject(SurveyorService);
  private toastService = inject(ToastService);

  claim = signal<ClaimDto | null>(null);

  dmgType = signal<DamageType>('');
  desc = '';
  cost = '';
  pav = '';
  driveable = signal<boolean | null>(null);
  workshop = '';
  notes = '';

  photos = signal<File[]>([]);
  docs = signal<File[]>([]);

  dmgTypeErr = signal('');
  descErr = signal('');
  costErr = signal('');
  driveableErr = signal('');
  photoErr = signal('');

  submitting = signal(false);
  showSuccess = signal(false);

  damageTypes: DamageType[] = ['Partial loss', 'Total loss', 'Third party', 'Theft'];

  ngOnInit(): void {
    const claimId = this.route.snapshot.params['id'];
    this.surveyorService.getAssignedClaims().subscribe({
      next: claims => {
        const found = claims.find(c => c.id.toString() === claimId);
        if (!found) {
          this.router.navigate(['/surveyor/claims']);
          return;
        }

        if (this.isSurveyReportLocked(found.status)) {
          this.toastService.warning('This claim no longer accepts survey reports.');
          this.router.navigate(['/surveyor/claims']);
          return;
        }

        this.claim.set(found);
      },
      error: () => this.router.navigate(['/surveyor/claims']),
    });
  }

  goBack(): void {
    this.router.navigate(['/surveyor/claims']);
  }

  onPhotoChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      const files = Array.from(input.files);
      this.photos.update(prev => [...prev, ...files].slice(0, 10));
      this.photoErr.set('');
    }
    input.value = '';
  }

  onDocChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      const files = Array.from(input.files);
      this.docs.update(prev => [...prev, ...files].slice(0, 5));
    }
    input.value = '';
  }

  removePhoto(index: number): void {
    this.photos.update(prev => prev.filter((_, i) => i !== index));
  }

  removeDoc(index: number): void {
    this.docs.update(prev => prev.filter((_, i) => i !== index));
  }

  selectDamageType(type: DamageType): void {
    this.dmgType.set(type);
    this.dmgTypeErr.set('');
  }

  setDriveable(value: boolean): void {
    this.driveable.set(value);
    this.driveableErr.set('');
  }

  validate(): boolean {
    let ok = true;
    if (!this.dmgType()) { this.dmgTypeErr.set('Please choose a damage type.'); ok = false; }
    else this.dmgTypeErr.set('');
    if (!this.desc.trim()) { this.descErr.set('Please describe the damage observed.'); ok = false; }
    else this.descErr.set('');
    if (!this.cost || parseFloat(this.cost) <= 0) { this.costErr.set('Please enter a valid estimated repair cost.'); ok = false; }
    else this.costErr.set('');
    if (this.driveable() === null) { this.driveableErr.set('Please indicate whether the vehicle is driveable.'); ok = false; }
    else this.driveableErr.set('');
    if (this.photos().length === 0) { this.photoErr.set('Please upload at least one damage photo.'); ok = false; }
    else this.photoErr.set('');
    return ok;
  }

  submit(): void {
    if (this.submitting()) return;
    const c = this.claim();
    if (!c || this.isSurveyReportLocked(c.status)) {
      this.toastService.warning('This claim no longer accepts survey reports.');
      this.router.navigate(['/surveyor/claims']);
      return;
    }

    if (!this.validate()) {
      this.toastService.warning('Please fill in all required fields.');
      return;
    }

    this.submitting.set(true);

    const remarks = [
      `Damage type: ${this.dmgType()}`,
      `Description: ${this.desc}`,
      `Driveable: ${this.driveable() === true ? 'Yes' : this.driveable() === false ? 'No' : 'N/A'}`,
      this.workshop ? `Workshop: ${this.workshop}` : '',
      this.notes ? `Notes: ${this.notes}` : '',
    ].filter(Boolean).join('\n');

    const firstPhoto = this.photos()[0];

    this.surveyorService.submitSurveyReport(c.id.toString(), {
      estimatedRepairCost: parseFloat(this.cost),
      surveyDate: new Date().toISOString(),
      remarks,
      reportDocument: firstPhoto,
      photos: this.photos(),
    }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.showSuccess.set(true);
      },
      error: () => {
        this.submitting.set(false);
        this.toastService.error('Failed to submit report. Please try again.');
      },
    });
  }

  onDone(): void {
    this.router.navigate(['/surveyor/claims']);
  }

  formatINR(value: number): string {
    if (value == null) return '₹ 0.00';
    const [integer, decimal] = Math.abs(value).toFixed(2).split('.');
    const lastThree = integer.slice(-3);
    const rest = integer.slice(0, -3);
    const formatted = rest.replace(/\B(?=(\d{2})+(?!\d))/g, ',');
    return '₹' + (formatted ? formatted + ',' : '') + lastThree + '.' + decimal;
  }

  private isSurveyReportLocked(status: string): boolean {
    return ['Approved', 'PayoutProcessed', 'Rejected', 'Settled', 'Withdrawn'].includes(status);
  }
}
