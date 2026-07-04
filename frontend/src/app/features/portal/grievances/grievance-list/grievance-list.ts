import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { GrievanceService } from '../services/grievance.service';
import { GrievanceDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-grievance-list',
  standalone: true,
  imports: [StatusBadgeComponent, EmptyStateComponent, DateFormatPipe],
  templateUrl: './grievance-list.html',
})
export class GrievanceListComponent implements OnInit {
  private readonly grievanceService = inject(GrievanceService);
  router = inject(Router);
  grievances = signal<GrievanceDto[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.grievanceService.getMyGrievances().subscribe({
      next: data => { this.grievances.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
