import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ClaimService } from '../services/claim.service';
import { ClaimDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-claim-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './claim-list.html',
})
export class ClaimListComponent implements OnInit {
  private claimService = inject(ClaimService);
  router = inject(Router);
  claims = signal<ClaimDto[]>([]);
  loading = signal(true);
  statusFilter = '';
  typeFilter = '';

  ngOnInit(): void { this.load(); }

  onFilterChange(): void {
    this.loading.set(true);
    this.load();
  }

  private load(): void {
    this.claimService.getMyClaims(this.statusFilter || undefined, this.typeFilter || undefined).subscribe({
      next: data => { this.claims.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  domainBgClass(type: string): string {
    const map: Record<string, string> = { HEALTH: 'bg-success-bg', MOTOR: 'bg-info-bg', LIFE: 'bg-[#F3EEFF]', ACCIDENT: 'bg-warning-bg', DEATH: 'bg-danger-bg', THEFT: 'bg-info-bg' };
    return map[type?.toUpperCase()] ?? 'bg-surface-alt';
  }

  domainFgClass(type: string): string {
    const map: Record<string, string> = { HEALTH: 'text-success', MOTOR: 'text-info', LIFE: 'text-[#7C3AED]', ACCIDENT: 'text-warning', DEATH: 'text-danger', THEFT: 'text-info' };
    return map[type?.toUpperCase()] ?? 'text-muted';
  }

  claimTypeAbbr(type: string): string {
    const map: Record<string, string> = { HEALTH: 'H', MOTOR: 'M', LIFE: 'L', ACCIDENT: 'A', DEATH: 'D', THEFT: 'T' };
    return map[type?.toUpperCase()] ?? type?.charAt(0) ?? '?';
  }
}
