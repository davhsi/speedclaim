import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ClaimService } from '../services/claim.service';
import { ClaimDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-claim-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe, PaginationComponent, FormsModule],
  templateUrl: './claim-list.html',
})
export class ClaimListComponent implements OnInit {
  private readonly claimService = inject(ClaimService);
  router = inject(Router);
  claims = signal<ClaimDto[]>([]);
  loading = signal(true);
  statusFilter = '';
  typeFilter = '';
  currentPage = signal(1);
  readonly pageSize = 10;

  totalPages = computed(() => Math.max(1, Math.ceil(this.claims().length / this.pageSize)));
  pagedClaims = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.claims().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }

  ngOnInit(): void { this.load(); }

  onFilterChange(): void {
    this.currentPage.set(1);
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
