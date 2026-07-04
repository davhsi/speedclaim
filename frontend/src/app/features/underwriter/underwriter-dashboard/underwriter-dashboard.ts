import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';
import { UnderwriterService } from '../services/underwriter.service';
import { AuthService } from '../../../core/services/auth.service';
import { ProductDto, ProposalDto } from '../../../core/models/api.models';
import { ProductService } from '../../portal/products/services/product.service';

@Component({
  selector: 'app-underwriter-dashboard',
  standalone: true,
  imports: [StatCardComponent, SafeHtmlPipe],
  templateUrl: './underwriter-dashboard.html',
})
export class UnderwriterDashboardComponent implements OnInit {
  private readonly uwService = inject(UnderwriterService);
  private readonly authService = inject(AuthService);
  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);

  pendingProposals = signal(0);
  pendingKyc = signal(0);
  pendingEndorsements = signal(0);
  activePolicies = signal(0);
  allProposals = signal<ProposalDto[]>([]);
  products = signal<ProductDto[]>([]);
  recentActivity = signal<{ title: string; subtitle: string; time: string; icon: string; bgClass: string }[]>([]);

  iconProposal = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/></svg>';
  iconKyc = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="8.5" cy="7" r="4"/><polyline points="17 11 19 13 23 7"/></svg>';
  iconEndorsement = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 013 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>';
  iconPolicy = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 016.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z"/></svg>';

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.productService.getAll().subscribe({
      next: products => {
        this.products.set(products);
        this.buildRecentActivity(this.allProposals());
      },
    });

    this.uwService.getAllProposals().subscribe({
      next: (proposals) => {
        this.allProposals.set(proposals);
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

    this.recentActivity.set(sorted.map(p => {
      const { color, bgClass } = this.statusStyle(p.status);
      return {
        title: `Proposal ${p.proposalNumber}`,
        subtitle: `${this.productName(p)} · ${p.status}`,
        time: this.formatRelativeTime(p.createdAt),
        icon: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="${color}" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/></svg>`,
        bgClass,
      };
    }));
  }

  private statusStyle(status: string): { color: string; bgClass: string } {
    if (status === 'Approved') {
      return { color: 'var(--color-success)', bgClass: 'bg-success-bg' };
    }
    if (status === 'Rejected') {
      return { color: 'var(--color-danger)', bgClass: 'bg-danger-bg' };
    }
    return { color: 'var(--color-warning)', bgClass: 'bg-warning-bg' };
  }

  private productName(proposal: ProposalDto): string {
    return this.products().find(p => p.id === proposal.productId)?.productName
      ?? proposal.productName
      ?? 'Insurance product';
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

  greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
