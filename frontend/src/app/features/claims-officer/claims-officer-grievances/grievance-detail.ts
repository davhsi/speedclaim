import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { ClaimsOfficerService } from '../services/claims-officer.service';
import { GrievanceDto } from '../../../core/models/api.models';
import { GrievanceStatus } from '../../../core/models/enums';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-grievance-detail',
  standalone: true,
  imports: [FormsModule, StatusBadgeComponent, DateFormatPipe],
  templateUrl: './grievance-detail.html',
})
export class GrievanceDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private claimsService = inject(ClaimsOfficerService);
  private authService = inject(AuthService);
  private toast = inject(ToastService);

  grievance = signal<GrievanceDto | null>(null);
  notes = '';
  selectedStatus: GrievanceStatus = 'Open';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadGrievance(id);
  }

  private loadGrievance(id: string): void {
    this.claimsService.getGrievanceById(id).subscribe({
      next: (g) => {
        this.grievance.set(g);
        this.selectedStatus = g.status;
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/claims-officer/grievances']);
  }

  formatCategory(cat: string): string {
    const map: Record<string, string> = {
      ClaimDelay: 'Claim Delay', PolicyServicing: 'Policy Servicing',
      PremiumIssue: 'Premium Issue', MisSelling: 'Mis-selling',
      AgentMisconduct: 'Agent Misconduct', Other: 'Other',
    };
    return map[cat] ?? cat;
  }

  isTerminal(g: GrievanceDto): boolean {
    return g.status === 'Resolved' || g.status === 'Closed';
  }

  onAssignToSelf(): void {
    const g = this.grievance();
    const user = this.authService.currentUser();
    if (!g || !user || this.isTerminal(g)) return;
    this.claimsService.assignGrievance(g.id, { assignedToId: user.id }).subscribe({
      next: () => {
        this.toast.success('Grievance assigned to you');
        this.loadGrievance(g.id);
      },
      error: () => this.toast.error('Failed to assign grievance'),
    });
  }

  onUpdateStatus(): void {
    const g = this.grievance();
    if (!g || this.isTerminal(g)) return;
    this.claimsService.updateGrievanceStatus(g.id, {
      status: this.selectedStatus,
      resolutionNotes: this.notes || undefined,
    }).subscribe({
      next: () => {
        this.toast.success('Grievance status updated');
        this.loadGrievance(g.id);
      },
      error: () => this.toast.error('Failed to update status'),
    });
  }

  onSaveNotes(): void {
    const g = this.grievance();
    if (!g || this.isTerminal(g) || !this.notes.trim()) return;
    this.claimsService.updateGrievanceStatus(g.id, {
      status: g.status,
      resolutionNotes: this.notes,
    }).subscribe({
      next: () => {
        this.toast.success('Notes saved');
        this.loadGrievance(g.id);
        this.notes = '';
      },
      error: () => this.toast.error('Failed to save notes'),
    });
  }
}
