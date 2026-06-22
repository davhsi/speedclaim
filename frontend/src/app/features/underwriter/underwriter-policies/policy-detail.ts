import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { UnderwriterService } from '../services/underwriter.service';
import { PolicyDto, PolicyStatusHistoryDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-uw-policy-detail',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './policy-detail.html',
})
export class PolicyDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private uwService = inject(UnderwriterService);

  policy = signal<PolicyDto | null>(null);
  history = signal<PolicyStatusHistoryDto[]>([]);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.uwService.getPolicyById(id).subscribe({
      next: (p) => this.policy.set(p),
    });
    this.uwService.getPolicyHistory(id).subscribe({
      next: (h) => this.history.set(h),
    });
  }

  getDotClass(status: string): string {
    const map: Record<string, string> = {
      Active: 'bg-success', Approved: 'bg-success',
      Pending: 'bg-warning', UnderReview: 'bg-warning',
      Lapsed: 'bg-danger', Cancelled: 'bg-danger', Expired: 'bg-danger',
    };
    return map[status] ?? 'bg-info';
  }

  goBack(): void {
    this.router.navigate(['/underwriter/policies']);
  }
}
