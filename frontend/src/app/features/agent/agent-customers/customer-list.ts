import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-agent-customer-list',
  standalone: true,
  imports: [RouterLink, SkeletonLoaderComponent],
  templateUrl: './customer-list.html',
})
export class AgentCustomerListComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  customers = signal<AgentCustomerDto[]>([]);
  searchQuery = signal('');

  filteredCustomers = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const list = this.customers();
    if (!q) return list;
    return list.filter(c =>
      c.fullName.toLowerCase().includes(q) ||
      c.email.toLowerCase().includes(q) ||
      String(c.id).includes(q),
    );
  });

  ngOnInit(): void {
    this.agentService.getCustomers().subscribe({
      next: customers => {
        this.customers.set(customers);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  initials(c: AgentCustomerDto): string {
    return (c.firstName.charAt(0) + c.lastName.charAt(0)).toUpperCase();
  }

  avatarColor(id: string): string {
    const colors = ['#0F6E8C', '#F2784B', '#1F9D6B', '#D9920A', '#2D7FF9', '#7C3AED'];
    const hash = Array.from(id).reduce((sum, char) => sum + char.charCodeAt(0), 0);
    return colors[hash % colors.length];
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
