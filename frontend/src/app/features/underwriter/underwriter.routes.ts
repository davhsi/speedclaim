import { Routes } from '@angular/router';

export const underwriterRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./underwriter-layout/underwriter-layout').then(m => m.UnderwriterLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./underwriter-dashboard/underwriter-dashboard').then(m => m.UnderwriterDashboardComponent) },
      { path: 'proposals', loadComponent: () => import('./underwriter-proposals/proposal-list').then(m => m.ProposalListComponent) },
      { path: 'proposals/:id', loadComponent: () => import('./underwriter-proposals/proposal-detail').then(m => m.ProposalDetailComponent) },
      { path: 'kyc', loadComponent: () => import('./underwriter-kyc/kyc-list').then(m => m.KycListComponent) },
      { path: 'kyc/:userId', loadComponent: () => import('./underwriter-kyc/kyc-detail').then(m => m.KycDetailComponent) },
      { path: 'endorsements', loadComponent: () => import('./underwriter-endorsements/endorsement-list').then(m => m.EndorsementListComponent) },
      { path: 'policies', loadComponent: () => import('./underwriter-policies/policy-list').then(m => m.PolicyListComponent) },
      { path: 'policies/:id', loadComponent: () => import('./underwriter-policies/policy-detail').then(m => m.PolicyDetailComponent) },
      { path: 'profile', loadComponent: () => import('./underwriter-profile/underwriter-profile').then(m => m.UnderwriterProfileComponent) },
    ],
  },
];
