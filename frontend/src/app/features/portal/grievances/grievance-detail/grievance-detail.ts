import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { GrievanceService } from '../services/grievance.service';
import { GrievanceDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';
import { DocumentPreviewComponent, PreviewDoc } from '../../../../shared/components/document-preview/document-preview';

@Component({
  selector: 'app-grievance-detail',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent, DateFormatPipe, DocumentPreviewComponent],
  templateUrl: './grievance-detail.html',
})
export class GrievanceDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly grievanceService = inject(GrievanceService);
  router = inject(Router);

  grievance = signal<GrievanceDto | null>(null);
  loading = signal(true);
  previewDoc = signal<PreviewDoc | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.grievanceService.getById(id).subscribe({
      next: g => { this.grievance.set(g); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openPreview(path: string): void {
    this.previewDoc.set({ url: '/' + path, label: 'Supporting document' });
  }
  closePreview(): void { this.previewDoc.set(null); }
}
