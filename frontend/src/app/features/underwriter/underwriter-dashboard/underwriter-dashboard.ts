import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card';
import { UnderwriterService } from '../services/underwriter.service';
import { AuthService } from '../../../core/services/auth.service';
import { ProposalDto, EndorsementDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-underwriter-dashboard',
  standalone: true,
  imports: [StatCardComponent],
  templateUrl: './underwriter-dashboard.html',
})
export class UnderwriterDashboardComponent implements OnInit {
  private uwService = inject(UnderwriterService);
  private authService = inject(AuthService);
  private router = inject(Router);

  pendingProposals = signal(0);
  pendingKyc = signal(0);
  pendingEndorsements = signal(0);
  activePolicies = signal(0);
  recentActivity = signal<{ title: string; subtitle: string; time: string; abbr: string; bgClass: string }[]>([]);

  iconProposal = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/></svg>';
  iconKyc = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="8.5" cy="7" r="4"/><polyline points="17 11 19 13 23 7"/></svg>';
  iconEndorsement = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 013 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>';
  iconPolicy = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 016.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z"/></svg>';

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.uwService.getAllProposals().subscribe({
      next: (proposals) => {
        const pending = proposals.filter(p => p.status === 'Submitted' || p.status === 'UnderReview');
        this.pendingProposals.set(pending.length);
        this.buildRecentActivity(proposals);
      },
    });

    this.uwService.getPendingKyc(1, 1).subscribe({
      next: (res) => this.pendingKyc.set(res.totalRecords),
    });

    this.uwService.getPendingEndorsements(1, 1).subscribe({
      next: (res) => this.pendingEndorsements.set(res.totalRecords),
    });

    this.uwService.getAllPolicies(1, 1).subscribe({
      next: (res) => this.activePolicies.set(res.totalRecords),
    });
  }

  private buildRecentActivity(proposals: ProposalDto[]): void {
    const sorted = [...proposals]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);

    this.recentActivity.set(sorted.map(p => ({
      title: `Proposal ${p.proposalNumber}`,
      subtitle: `${p.productName ?? 'Insurance'} · ${p.status}`,
      time: this.formatRelativeTime(p.createdAt),
      abbr: 'PRP',
      bgClass: p.status === 'Approved' ? 'bg-success-bg text-success' : p.status === 'Rejected' ? 'bg-danger-bg text-danger' : 'bg-warning-bg text-warning',
    })));
  }

  private formatRelativeTime(dateStr: string): string {
    const d = new Date(dateStr);
    const now = new Date();
    const diff = now.getTime() - d.getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 60) return `${mins}m ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 7) return `${days}d ago`;
    return d.toLocaleDateString('en-IN', { day: 'numeric', month: 'short' });
  }

  firstName(): string {
    return this.authService.currentUser()?.firstName ?? '';
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
