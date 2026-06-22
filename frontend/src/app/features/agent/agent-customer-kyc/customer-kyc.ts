import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { KycRecordDto } from '../../../core/models/api.models';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-agent-customer-kyc',
  standalone: true,
  imports: [RouterLink, FileUploadComponent],
  templateUrl: './customer-kyc.html',
})
export class AgentCustomerKycComponent implements OnInit {
  private agentService = inject(AgentService);
  private http = inject(HttpClient);
  private toast = inject(ToastService);
  private router = inject(Router);

  customers = signal<AgentCustomerDto[]>([]);
  selectedCustomerId = signal<number | null>(null);
  existingKyc = signal<KycRecordDto | null>(null);
  idType = signal<'aadhaar' | 'pan'>('aadhaar');
  idNumber = '';
  frontFile: File | null = null;
  backFile: File | null = null;
  submitting = signal(false);

  ngOnInit(): void {
    this.agentService.getCustomers().subscribe(c => this.customers.set(c));
  }

  onCustomerChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value);
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
    if (!this.selectedCustomerId() || !this.frontFile || !this.idNumber) return;
    this.submitting.set(true);

    const fd = new FormData();
    fd.append('file', this.frontFile);
    fd.append('customerId', this.selectedCustomerId()!.toString());

    if (this.idType() === 'aadhaar') {
      fd.append('aadhaarNumber', this.idNumber);
      if (this.backFile) fd.append('backFile', this.backFile);
      this.http.post<KycRecordDto>('/api/v1/users/kyc/aadhaar', fd).subscribe({
        next: () => {
          this.toast.success('Aadhaar uploaded successfully');
          this.submitting.set(false);
          this.router.navigate(['/agent/customers']);
        },
        error: () => { this.toast.error('Upload failed'); this.submitting.set(false); },
      });
    } else {
      fd.append('panNumber', this.idNumber);
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
