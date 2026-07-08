import { Routes } from '@angular/router';

export const agentRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./agent-layout/agent-layout').then(m => m.AgentLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./agent-dashboard/agent-dashboard').then(m => m.AgentDashboardComponent) },
      { path: 'customers', loadComponent: () => import('./agent-customers/customer-list').then(m => m.AgentCustomerListComponent) },
      { path: 'customers/new', loadComponent: () => import('./agent-customers/customer-add').then(m => m.AgentCustomerAddComponent) },
      { path: 'customers/:id', loadComponent: () => import('./agent-customers/customer-detail').then(m => m.AgentCustomerDetailComponent) },
      { path: 'proposals/new', loadComponent: () => import('./agent-proposals/proposal-submit').then(m => m.AgentProposalSubmitComponent) },
      { path: 'proposals/:id', loadComponent: () => import('./agent-proposals/proposal-detail').then(m => m.AgentProposalDetailComponent) },
      { path: 'proposals', loadComponent: () => import('./agent-proposals/proposal-list').then(m => m.AgentProposalListComponent) },
      { path: 'policies', loadComponent: () => import('./agent-policies/policy-list').then(m => m.AgentPolicyListComponent) },
      { path: 'renewals', loadComponent: () => import('./agent-renewals/renewal-list').then(m => m.AgentRenewalListComponent) },
      { path: 'commissions', loadComponent: () => import('./agent-commissions/commission-list').then(m => m.AgentCommissionListComponent) },
      { path: 'customer-kyc', loadComponent: () => import('./agent-customer-kyc/customer-kyc').then(m => m.AgentCustomerKycComponent) },
      { path: 'profile', loadComponent: () => import('./agent-profile/agent-profile').then(m => m.AgentProfileComponent) },
    ],
  },
];
