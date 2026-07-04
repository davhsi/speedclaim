import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { KycRecordDto } from '../../../core/models/api.models';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload';
import { ToastService } from '../../../shared/components/toast/toast.service';

const AADHAAR_PATTERN = /^\d{12}$/;
const PAN_PATTERN = /^[A-Z]{5}\d{4}[A-Z]$/;

@Component({
  selector: 'app-agent-customer-kyc',
  standalone: true,
  imports: [FormsModule, RouterLink, FileUploadComponent],
  templateUrl: './customer-kyc.html',
})
export class AgentCustomerKycComponent implements OnInit {
  private readonly agentService = inject(AgentService);
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  customers = signal<AgentCustomerDto[]>([]);
  selectedCustomerId = signal<string | null>(null);
  existingKyc = signal<KycRecordDto | null>(null);
  idType = signal<'aadhaar' | 'pan'>('aadhaar');
  idNumber = signal('');
  frontFile: File | null = null;
  backFile: File | null = null;
  submitting = signal(false);

  idValid = computed(() => {
    const v = this.idNumber().trim().toUpperCase();
    return this.idType() === 'aadhaar' ? AADHAAR_PATTERN.test(v) : PAN_PATTERN.test(v);
  });

  idError = computed(() => {
    const v = this.idNumber().trim();
    if (!v) return '';
    if (this.idValid()) return '';
    return this.idType() === 'aadhaar'
      ? 'Aadhaar must be exactly 12 digits.'
      : 'PAN must be in the format ABCDE1234F.';
  });

  ngOnInit(): void {
    this.agentService.getCustomers().subscribe(c => this.customers.set(c));
  }

  onCustomerChange(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    this.selectedCustomerId.set(id || null);
    this.existingKyc.set(null);
    if (id) {
      this.http.get<KycRecordDto>(`/api/v1/agents/customers/${id}/kyc`).subscribe({
        next: k => this.existingKyc.set(k),
        error: () => {},
      });
    }
  }

  submit(): void {
    if (this.submitting() || !this.selectedCustomerId() || !this.frontFile || !this.idValid()) return;
    this.submitting.set(true);

    const idValue = this.idNumber().trim().toUpperCase();
    const fd = new FormData();
    fd.append('frontDocument', this.frontFile);
    fd.append('customerId', this.selectedCustomerId()!.toString());

    if (this.idType() === 'aadhaar') {
      fd.append('aadhaarNumber', idValue);
      if (this.backFile) fd.append('backDocument', this.backFile);
      this.http.post<KycRecordDto>('/api/v1/users/kyc/aadhaar', fd).subscribe({
        next: () => {
          this.toast.success('Aadhaar uploaded successfully');
          this.submitting.set(false);
          this.router.navigate(['/agent/customers']);
        },
        error: () => { this.toast.error('Upload failed'); this.submitting.set(false); },
      });
    } else {
      fd.append('panNumber', idValue);
      this.http.post<KycRecordDto>('/api/v1/users/kyc/pan', fd).subscribe({
        next: () => {
          this.toast.success('PAN uploaded successfully');
          this.submitting.set(false);
          this.router.navigate(['/agent/customers']);
        },
        error: () => { this.toast.error('Upload failed'); this.submitting.set(false); },
      });
    }
  }
}
