import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { PolicyService } from '../services/policy.service';
import { PolicyDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';

@Component({
  selector: 'app-policy-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe],
  templateUrl: './policy-list.html',
})
export class PolicyListComponent implements OnInit {
  private policyService = inject(PolicyService);
  router = inject(Router);

  policies = signal<PolicyDto[]>([]);
  loading = signal(true);

  ngOnInit(): void { this.load(); }

  filterByStatus(event: Event): void {
    const val = (event.target as HTMLSelectElement).value;
    this.loading.set(true);
    this.load(val || undefined);
  }

  private load(status?: string): void {
    this.policyService.getMyPolicies(status).subscribe({
      next: data => { this.policies.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  domainBgClass(domain: string): string {
    const map: Record<string, string> = { Health: 'bg-success-bg', Motor: 'bg-info-bg', Life: 'bg-[#F3EEFF]' };
    return map[domain] ?? 'bg-surface-alt';
  }

  domainIcon(domain: string): string {
    const map: Record<string, string> = {
      Health: '<svg width="23" height="23" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      Motor: '<svg width="23" height="23" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      Life: '<svg width="23" height="23" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
    };
    return map[domain] ?? '<svg width="23" height="23" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="10"/></svg>';
  }
}
