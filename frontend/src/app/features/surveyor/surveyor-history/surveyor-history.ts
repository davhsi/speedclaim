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
  host: { class: 'flex-1 min-h-0 flex flex-col' },
})
export class SurveyorHistoryComponent implements OnInit {
  private readonly surveyorService = inject(SurveyorService);

  claims = signal<ClaimDto[]>([]);
  loading = signal(true);
  currentPage = signal(1);
  readonly pageSize = 10;

  submittedClaims = computed(() =>
    this.claims().filter(c =>
      this.hasSubmittedSurveyReport(c) ||
      c.status === 'Settled' || c.status === 'Approved' || c.status === 'Withdrawn' || c.status === 'Rejected'
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
    const formatted = this.groupByTwos(rest);
    return '₹' + (formatted ? formatted + ',' : '') + lastThree + '.' + decimal;
  }

  // Groups digits in pairs from the right (Indian lakh/crore style), e.g. "12345" -> "1,23,45".
  // Implemented without a lookahead-quantifier regex to avoid superlinear backtracking.
  private groupByTwos(digits: string): string {
    if (!digits) return '';
    const parts: string[] = [];
    let i = digits.length;
    while (i > 2) {
      parts.unshift(digits.slice(i - 2, i));
      i -= 2;
    }
    parts.unshift(digits.slice(0, i));
    return parts.join(',');
  }

  private hasSubmittedSurveyReport(claim: ClaimDto): boolean {
    return Boolean(
      claim.surveyDate ||
      claim.surveyEstimatedCost != null ||
      claim.surveyorRemarks ||
      claim.documents?.some(d => d.documentKey === 'SurveyorReport')
    );
  }
}
