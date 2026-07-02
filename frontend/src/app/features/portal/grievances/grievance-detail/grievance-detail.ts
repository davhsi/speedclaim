import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { GrievanceService } from '../services/grievance.service';
import { GrievanceDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-grievance-detail',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent, DateFormatPipe],
  templateUrl: './grievance-detail.html',
})
export class GrievanceDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private grievanceService = inject(GrievanceService);
  router = inject(Router);

  grievance = signal<GrievanceDto | null>(null);
  loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.grievanceService.getById(id).subscribe({
      next: g => { this.grievance.set(g); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
