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
    const map: Record<string, string> = { Health: 'bg-success-bg', Motor: 'bg-info-bg', Life: 'bg-[#F3EEFF]', Accident: 'bg-warning-bg', Death: 'bg-danger-bg', Theft: 'bg-info-bg' };
    return map[type] ?? 'bg-surface-alt';
  }

  domainFgClass(type: string): string {
    const map: Record<string, string> = { Health: 'text-success', Motor: 'text-info', Life: 'text-[#7C3AED]', Accident: 'text-warning', Death: 'text-danger', Theft: 'text-info' };
    return map[type] ?? 'text-muted';
  }

  claimTypeAbbr(type: string): string {
    const map: Record<string, string> = { Health: 'H', Motor: 'M', Life: 'L', Accident: 'A', Death: 'D', Theft: 'T' };
    return map[type] ?? type.charAt(0);
  }
}
