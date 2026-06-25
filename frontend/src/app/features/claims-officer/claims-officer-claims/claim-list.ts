import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
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
  private claimsService = inject(ClaimsOfficerService);
  private router = inject(Router);

  claims = signal<ClaimDto[]>([]);
  currentPage = signal(1);
  totalPages = signal(1);
  totalRecords = signal(0);
  loading = signal(false);

  typeFilter = '';
  statusFilter = '';
  searchQuery = '';

  ngOnInit(): void {
    this.loadClaims();
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.loadClaims();
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
      Life: 'bg-warning-bg text-warning',
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
