import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { UnderwriterService } from '../services/underwriter.service';
import { PolicyDto } from '../../../core/models/api.models';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-uw-policy-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe, PaginationComponent, FormsModule],
  templateUrl: './policy-list.html',
})
export class PolicyListComponent implements OnInit {
  private uwService = inject(UnderwriterService);
  private router = inject(Router);

  allPolicies = signal<PolicyDto[]>([]);
  searchTerm = '';
  currentPage = signal(1);
  totalPages = signal(1);

  filteredPolicies = computed(() => {
    const term = this.searchTerm.toLowerCase().trim();
    const policies = this.allPolicies();
    if (!term) return policies;
    return policies.filter(p =>
      p.policyNumber.toLowerCase().includes(term) ||
      p.productName.toLowerCase().includes(term) ||
      p.status.toLowerCase().includes(term)
    );
  });

  ngOnInit(): void {
    this.loadPage(1);
  }

  loadPage(page: number): void {
    this.uwService.getAllPolicies(page, 20).subscribe({
      next: (res) => {
        this.allPolicies.set(res.data);
        this.currentPage.set(res.pageNumber);
        this.totalPages.set(res.totalPages);
      },
    });
  }

  onPageChange(page: number): void {
    this.loadPage(page);
  }

  openPolicy(id: string): void {
    this.router.navigate(['/underwriter/policies', id]);
  }
}
