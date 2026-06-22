import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { SurveyorService } from '../services/surveyor.service';
import { ClaimDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-surveyor-history',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './surveyor-history.html',
})
export class SurveyorHistoryComponent implements OnInit {
  private surveyorService = inject(SurveyorService);

  claims = signal<ClaimDto[]>([]);
  loading = signal(true);

  submittedClaims = computed(() =>
    this.claims().filter(c =>
      c.status === 'Settled' || c.status === 'Approved' || c.status === 'PayoutProcessed' || c.status === 'Rejected'
    )
  );

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
