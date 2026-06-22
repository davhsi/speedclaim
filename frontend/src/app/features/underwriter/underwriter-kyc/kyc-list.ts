import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { UnderwriterService, UnderwriterKycDto } from '../services/underwriter.service';

@Component({
  selector: 'app-uw-kyc-list',
  standalone: true,
  imports: [StatusBadgeComponent, DateFormatPipe, PaginationComponent],
  templateUrl: './kyc-list.html',
})
export class KycListComponent implements OnInit {
  private uwService = inject(UnderwriterService);
  private router = inject(Router);

  kycRecords = signal<UnderwriterKycDto[]>([]);
  currentPage = signal(1);
  totalPages = signal(1);
  totalRecords = signal(0);
  private pageSize = 10;

  ngOnInit(): void {
    this.loadPage(1);
  }

  loadPage(page: number): void {
    this.uwService.getPendingKyc(page, this.pageSize).subscribe({
      next: (res) => {
        this.kycRecords.set(res.data);
        this.currentPage.set(res.pageNumber);
        this.totalPages.set(res.totalPages);
        this.totalRecords.set(res.totalRecords);
      },
    });
  }

  onPageChange(page: number): void {
    this.loadPage(page);
  }

  openKyc(userId: string): void {
    this.router.navigate(['/underwriter/kyc', userId]);
  }
}
