import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { SurveyorService } from '../services/surveyor.service';
import { ClaimDto } from '../../../core/models/api.models';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-surveyor-history',
  standalone: true,
  imports: [DatePipe, PaginationComponent, SkeletonLoaderComponent],
  templateUrl: './surveyor-history.html',
})
export class SurveyorHistoryComponent implements OnInit {
  private surveyorService = inject(SurveyorService);

  claims = signal<ClaimDto[]>([]);
  loading = signal(true);
  currentPage = signal(1);
  readonly pageSize = 10;

  submittedClaims = computed(() =>
    this.claims().filter(c =>
      c.status === 'Settled' || c.status === 'Approved' || c.status === 'PayoutProcessed' || c.status === 'Rejected'
    )
  );

  totalPages = computed(() => Math.max(1, Math.ceil(this.submittedClaims().length / this.pageSize)));
  pagedHistory = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.submittedClaims().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }

  ngOnInit(): void {
    this.surveyorService.getAssignedClaims().subscribe({
      next: claims => {
        this.claims.set(claims);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  formatINR(value: number): string {
    if (value == null) return '₹ 0.00';
    const [integer, decimal] = Math.abs(value).toFixed(2).split('.');
    const lastThree = integer.slice(-3);
    const rest = integer.slice(0, -3);
    const formatted = rest.replace(/\B(?=(\d{2})+(?!\d))/g, ',');
    return '₹' + (formatted ? formatted + ',' : '') + lastThree + '.' + decimal;
  }
}
