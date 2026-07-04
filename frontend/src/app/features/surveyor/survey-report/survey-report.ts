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
  host: { class: 'flex-1 min-h-0 flex flex-col' },
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
  reportDocument = signal<File | null>(null);

  dmgTypeErr = signal('');
  descErr = signal('');
  costErr = signal('');
  driveableErr = signal('');
  photoErr = signal('');
  reportDocumentErr = signal('');

  submitting = signal(false);
  showSuccess = signal(false);

  damageTypes: DamageType[] = ['Partial loss', 'Total loss', 'Third party', 'Theft'];

  private get draftKey(): string { return `survey_draft_${this.claim()?.id ?? 'unknown'}`; }

  saveDraft(): void {
    const c = this.claim();
    if (!c) return;
    try {
      localStorage.setItem(this.draftKey, JSON.stringify({
        dmgType: this.dmgType(), desc: this.desc, cost: this.cost,
        pav: this.pav, driveable: this.driveable(), workshop: this.workshop, notes: this.notes,
      }));
    } catch { /* storage full or unavailable */ }
  }

  private loadDraft(): void {
    try {
      const raw = localStorage.getItem(this.draftKey);
      if (!raw) return;
      const d = JSON.parse(raw);
      if (d.dmgType) this.dmgType.set(d.dmgType);
      this.desc = d.desc ?? '';
      this.cost = d.cost ?? '';
      this.pav = d.pav ?? '';
      if (d.driveable !== null && d.driveable !== undefined) this.driveable.set(d.driveable);
      this.workshop = d.workshop ?? '';
      this.notes = d.notes ?? '';
      if (Object.values(d).some(Boolean)) this.toastService.success('Draft restored — pick up where you left off.');
    } catch { /* corrupted draft */ }
  }

  private clearDraft(): void {
    try { localStorage.removeItem(this.draftKey); } catch { /* noop */ }
  }

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
        this.loadDraft();
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
      this.reportDocument.set(input.files[0]);
      this.reportDocumentErr.set('');
    }
    input.value = '';
  }

  removePhoto(index: number): void {
    this.photos.update(prev => prev.filter((_, i) => i !== index));
  }

  removeDoc(): void {
    this.reportDocument.set(null);
  }

  selectDamageType(type: DamageType): void {
    this.dmgType.set(type);
    this.dmgTypeErr.set('');
    this.saveDraft();
  }

  setDriveable(value: boolean): void {
    this.driveable.set(value);
    this.driveableErr.set('');
    this.saveDraft();
  }

  validate(): boolean {
    let ok = true;
    if (this.dmgType()) this.dmgTypeErr.set('');
    else { this.dmgTypeErr.set('Please choose a damage type.'); ok = false; }
    if (!this.desc.trim()) { this.descErr.set('Please describe the damage observed.'); ok = false; }
    else this.descErr.set('');
    if (!this.cost || Number.parseFloat(this.cost) <= 0) { this.costErr.set('Please enter a valid estimated repair cost.'); ok = false; }
    else this.costErr.set('');
    if (this.driveable() === null) { this.driveableErr.set('Please indicate whether the vehicle is driveable.'); ok = false; }
    else this.driveableErr.set('');
    if (this.photos().length === 0) { this.photoErr.set('Please upload at least one damage photo.'); ok = false; }
    else this.photoErr.set('');
    if (this.reportDocument()) this.reportDocumentErr.set('');
    else { this.reportDocumentErr.set('Please upload the survey report document.'); ok = false; }
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
      this.pav ? `Pre-accident market value: ₹${Number.parseFloat(this.pav).toLocaleString('en-IN', { minimumFractionDigits: 2 })}` : '',
      this.workshop ? `Workshop: ${this.workshop}` : '',
      this.notes ? `Notes: ${this.notes}` : '',
    ].filter(Boolean).join('\n');

    const reportDocument = this.reportDocument();
    if (!reportDocument) return;

    this.surveyorService.submitSurveyReport(c.id.toString(), {
      estimatedRepairCost: Number.parseFloat(this.cost),
      surveyDate: new Date().toISOString(),
      remarks,
      reportDocument,
      photos: this.photos(),
    }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.clearDraft();
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
    return ['Approved', 'Rejected', 'Settled', 'Withdrawn'].includes(status);
  }
}
