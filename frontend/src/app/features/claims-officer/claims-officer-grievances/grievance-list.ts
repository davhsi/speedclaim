import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { ClaimsOfficerService } from '../services/claims-officer.service';
import { GrievanceDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-grievance-list',
  standalone: true,
  imports: [StatusBadgeComponent, PaginationComponent, DateFormatPipe],
  templateUrl: './grievance-list.html',
})
export class GrievanceListComponent implements OnInit {
  private claimsService = inject(ClaimsOfficerService);
  private router = inject(Router);

  grievances = signal<GrievanceDto[]>([]);
  currentPage = signal(1);
  totalPages = signal(1);
  totalRecords = signal(0);

  ngOnInit(): void {
    this.loadGrievances();
  }

  onPageChange(page: number): void {
    this.currentPage.set(page);
    this.loadGrievances();
  }

  private loadGrievances(): void {
    this.claimsService.getAllGrievances(this.currentPage(), 20).subscribe({
      next: (res) => {
        this.grievances.set(res.data);
        this.totalPages.set(res.totalPages);
        this.totalRecords.set(res.totalRecords);
      },
    });
  }

  formatCategory(cat: string): string {
    const map: Record<string, string> = {
      ClaimDelay: 'Claim Delay', PolicyServicing: 'Policy Servicing',
      PremiumIssue: 'Premium Issue', MisSelling: 'Mis-selling',
      AgentMisconduct: 'Agent Misconduct', Other: 'Other',
    };
    return map[cat] ?? cat;
  }

  openGrievance(id: number): void {
    this.router.navigate(['/claims-officer/grievances', id]);
  }
}
