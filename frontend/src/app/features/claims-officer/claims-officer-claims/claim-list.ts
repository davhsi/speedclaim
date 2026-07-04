import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { ClaimsOfficerService } from '../services/claims-officer.service';
import { ClaimDto } from '../../../core/models/api.models';
import { ClaimStatus, ClaimType } from '../../../core/models/enums';

@Component({
  selector: 'app-claim-list',
  standalone: true,
  imports: [FormsModule, StatusBadgeComponent, PaginationComponent, EmptyStateComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './claim-list.html',
})
export class ClaimListComponent implements OnInit {
  private readonly claimsService = inject(ClaimsOfficerService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  claims = signal<ClaimDto[]>([]);
  currentPage = signal(1);
  totalPages = signal(1);
  totalRecords = signal(0);
  loading = signal(false);
  filteredClaims = computed(() => {
    const q = this.searchQuery.trim().toLowerCase();
    if (!q) return this.claims();
    return this.claims().filter(claim =>
      claim.claimNumber.toLowerCase().includes(q) ||
      (claim.customerName ?? '').toLowerCase().includes(q) ||
      (claim.policyNumber ?? '').toLowerCase().includes(q) ||
      claim.id.toLowerCase().includes(q)
    );
  });

  typeFilter = '';
  statusFilter = '';
  searchQuery = '';

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams;
    if (params['status']) this.statusFilter = params['status'];
    if (params['type']) this.typeFilter = params['type'];
    this.loadClaims();
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.loadClaims();
  }

  onSearchChange(): void {
    this.currentPage.set(1);
  }

  onPageChange(page: number): void {
    this.currentPage.set(page);
    this.loadClaims();
  }

  private loadClaims(): void {
    this.loading.set(true);
    const status = this.statusFilter as ClaimStatus | undefined || undefined;
    const type = this.typeFilter as ClaimType | undefined || undefined;
    this.claimsService.getAllClaims(this.currentPage(), 20, status, type).subscribe({
      next: (res) => {
        this.claims.set(res.data);
        this.totalPages.set(res.totalPages);
        this.totalRecords.set(res.totalRecords);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  getTypePillClass(type: string): string {
    const map: Record<string, string> = {
      Motor: 'bg-info-bg text-info',
      Health: 'bg-success-bg text-success',
      Death: 'bg-surface-alt text-muted',
      Maturity: 'bg-surface-alt text-muted',
      Accident: 'bg-info-bg text-info',
      Theft: 'bg-danger-bg text-danger',
      NaturalDamage: 'bg-warning-bg text-warning',
    };
    return map[type] ?? 'bg-surface text-muted';
  }

  openClaim(id: string): void {
    this.router.navigate(['/claims-officer/claims', id]);
  }
}
