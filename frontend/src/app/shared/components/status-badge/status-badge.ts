import { Component, input, computed } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  templateUrl: './status-badge.html',
  styles: `
    :host span:first-child {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      padding: 4px 9px;
      border-radius: 999px;
      font-size: 10px;
      font-weight: 600;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      white-space: nowrap;
    }
  `,
})
export class StatusBadgeComponent {
  status = input.required<string>();

  displayLabel = computed(() => {
    const s = this.status();
    const labels: Record<string, string> = {
      UnderReview: 'Under Review',
      PreAuthRequested: 'Pre-Auth Requested',
      PreAuthApproved: 'Pre-Auth Approved',
      DocumentsPending: 'Docs Pending',
      InProgress: 'In Progress',
    };
    return labels[s] ?? s;
  });

  classes = computed(() => {
    const s = this.status();
    const map: Record<string, string> = {
      Active: 'bg-success-bg text-success border border-success-border',
      Approved: 'bg-success-bg text-success border border-success-border',
      Paid: 'bg-success-bg text-success border border-success-border',
      Settled: 'bg-success-bg text-success border border-success-border',
      Resolved: 'bg-success-bg text-success border border-success-border',

      Pending: 'bg-warning-bg text-warning border border-warning-border',
      UnderReview: 'bg-warning-bg text-warning border border-warning-border',
      PreAuthRequested: 'bg-warning-bg text-warning border border-warning-border',
      InProgress: 'bg-warning-bg text-warning border border-warning-border',
      Due: 'bg-warning-bg text-warning border border-warning-border',
      Submitted: 'bg-warning-bg text-warning border border-warning-border',
      Intimated: 'bg-warning-bg text-warning border border-warning-border',
      Open: 'bg-warning-bg text-warning border border-warning-border',

      Rejected: 'bg-danger-bg text-danger border border-danger-border',
      Cancelled: 'bg-danger-bg text-danger border border-danger-border',
      Failed: 'bg-danger-bg text-danger border border-danger-border',
      Overdue: 'bg-danger-bg text-danger border border-danger-border',
      Lapsed: 'bg-danger-bg text-danger border border-danger-border',
      Withdrawn: 'bg-danger-bg text-danger border border-danger-border',
      Escalated: 'bg-danger-bg text-danger border border-danger-border',

      Assigned: 'bg-info-bg text-info border border-info-border',
      PreAuthApproved: 'bg-info-bg text-info border border-info-border',
      DocumentsPending: 'bg-info-bg text-info border border-info-border',

      Draft: 'bg-[#F0F1F3] text-muted border border-[#D1D5DB]',
      Expired: 'bg-[#F0F1F3] text-muted border border-[#D1D5DB]',
      Closed: 'bg-[#F0F1F3] text-muted border border-[#D1D5DB]',
      Claimed: 'bg-[#F0F1F3] text-muted border border-[#D1D5DB]',
    };
    return map[s] ?? 'bg-[#F0F1F3] text-muted border border-[#D1D5DB]';
  });

  dotClass = computed(() => {
    const s = this.status();
    const map: Record<string, string> = {
      Active: 'bg-success', Approved: 'bg-success', Paid: 'bg-success', Settled: 'bg-success', Resolved: 'bg-success',
      Pending: 'bg-warning', UnderReview: 'bg-warning', PreAuthRequested: 'bg-warning', InProgress: 'bg-warning',
      Due: 'bg-warning', Submitted: 'bg-warning', Intimated: 'bg-warning', Open: 'bg-warning',
      Rejected: 'bg-danger', Cancelled: 'bg-danger', Failed: 'bg-danger', Overdue: 'bg-danger',
      Lapsed: 'bg-danger', Withdrawn: 'bg-danger', Escalated: 'bg-danger',
      Assigned: 'bg-info', PreAuthApproved: 'bg-info', DocumentsPending: 'bg-info',
    };
    return map[s] ?? 'bg-muted';
  });
}
