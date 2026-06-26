import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { SurveyorService } from '../services/surveyor.service';
import { SurveyorLayoutComponent } from '../surveyor-layout/surveyor-layout';
import { ClaimDto } from '../../../core/models/api.models';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';

type Tab = 'all' | 'pending' | 'overdue' | 'submitted';

@Component({
  selector: 'app-surveyor-claims',
  standalone: true,
  imports: [DatePipe, PaginationComponent],
  templateUrl: './surveyor-claims.html',
})
export class SurveyorClaimsComponent implements OnInit {
  layout = inject(SurveyorLayoutComponent);
  router = inject(Router);
  private authService = inject(AuthService);
  private surveyorService = inject(SurveyorService);

  claims = signal<ClaimDto[]>([]);
  loading = signal(true);
  activeTab = signal<Tab>('all');
  currentPage = signal(1);
  readonly pageSize = 10;

  tabs: { key: Tab; label: string }[] = [
    { key: 'all', label: 'All' },
    { key: 'pending', label: 'Pending' },
    { key: 'overdue', label: 'Overdue' },
    { key: 'submitted', label: 'Submitted' },
  ];

  filteredClaims = computed(() => {
    const all = this.claims();
    const tab = this.activeTab();
    if (tab === 'all') return all;
    return all.filter(c => {
      const s = this.mapSurveyStatus(c);
      if (tab === 'pending') return s === 'Pending';
      if (tab === 'overdue') return s === 'Overdue';
      if (tab === 'submitted') return s === 'Submitted';
      return true;
    });
  });

  pendingCount = computed(() => this.claims().filter(c => this.mapSurveyStatus(c) === 'Pending').length);
  overdueCount = computed(() => this.claims().filter(c => this.mapSurveyStatus(c) === 'Overdue').length);
  submittedCount = computed(() => this.claims().filter(c => this.mapSurveyStatus(c) === 'Submitted').length);

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredClaims().length / this.pageSize)));
  pagedClaims = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredClaims().slice(start, start + this.pageSize);
  });

  onTabChange(tab: Tab): void { this.activeTab.set(tab); this.currentPage.set(1); }
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

  mapSurveyStatus(claim: ClaimDto): string {
    if (claim.status === 'Settled' || claim.status === 'Approved' || claim.status === 'PayoutProcessed') return 'Submitted';
    if (claim.status === 'Rejected') return 'Submitted';
    const intimated = new Date(claim.intimationDate);
    const now = new Date();
    const days = Math.floor((now.getTime() - intimated.getTime()) / 86400000);
    if (days > 7) return 'Overdue';
    return 'Pending';
  }

  openClaim(claim: ClaimDto): void {
    const status = this.mapSurveyStatus(claim);
    if (status === 'Submitted') return;
    this.router.navigate(['/surveyor/claims', claim.id, 'report']);
  }

  barColor(status: string): string {
    const s = this.mapSurveyStatus({ status } as ClaimDto);
    const m: Record<string, string> = { Pending: '#D9920A', Overdue: '#D14343', Submitted: '#1F9D6B' };
    return m[s] ?? '#C5CBD3';
  }

  statusClasses(status: string): string {
    const s = this.mapSurveyStatus({ status } as ClaimDto);
    const m: Record<string, string> = {
      Pending: 'bg-warning-bg text-warning border-warning-border',
      Overdue: 'bg-danger-bg text-danger border-danger-border',
      Submitted: 'bg-success-bg text-success border-success-border',
    };
    return m[s] ?? 'bg-[#F0F1F3] text-muted border-[#D1D5DB]';
  }

  displayStatus(status: string): string {
    return this.mapSurveyStatus({ status } as ClaimDto);
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
