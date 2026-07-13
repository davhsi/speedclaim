import { Component, inject, signal, computed, OnInit } from '@angular/core';
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
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly claimsService = inject(ClaimsOfficerService);
  private readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);

  grievance = signal<GrievanceDto | null>(null);
  pendingAction = signal<'assign' | 'status' | 'notes' | null>(null);
  actionInFlight = computed(() => this.pendingAction() !== null);
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
    if (!g || !user || this.isTerminal(g) || this.actionInFlight()) return;
    this.pendingAction.set('assign');
    this.claimsService.assignGrievance(g.id, { assignedToId: user.id }).subscribe({
      next: () => {
        this.toast.success('Grievance assigned to you');
        this.loadGrievance(g.id);
        this.pendingAction.set(null);
      },
      error: () => {
        this.toast.error('Failed to assign grievance');
        this.pendingAction.set(null);
      },
    });
  }

  onUpdateStatus(): void {
    const g = this.grievance();
    if (!g || this.isTerminal(g) || this.actionInFlight()) return;
    if ((this.selectedStatus === 'Resolved' || this.selectedStatus === 'Closed') && !this.notes.trim()) {
      this.toast.warning('Resolution notes are required before resolving or closing a grievance.');
      return;
    }
    this.pendingAction.set('status');
    this.claimsService.updateGrievanceStatus(g.id, {
      status: this.selectedStatus,
      resolutionNotes: this.notes.trim() || undefined,
    }).subscribe({
      next: () => {
        this.toast.success('Grievance status updated');
        this.loadGrievance(g.id);
        this.pendingAction.set(null);
      },
      error: () => {
        this.toast.error('Failed to update status');
        this.pendingAction.set(null);
      },
    });
  }

  onSaveNotes(): void {
    const g = this.grievance();
    if (!g || this.isTerminal(g) || !this.notes.trim() || this.actionInFlight()) return;
    this.pendingAction.set('notes');
    this.claimsService.updateGrievanceStatus(g.id, {
      status: g.status,
      resolutionNotes: this.notes.trim(),
    }).subscribe({
      next: () => {
        this.toast.success('Notes saved');
        this.loadGrievance(g.id);
        this.notes = '';
        this.pendingAction.set(null);
      },
      error: () => {
        this.toast.error('Failed to save notes');
        this.pendingAction.set(null);
      },
    });
  }
}
