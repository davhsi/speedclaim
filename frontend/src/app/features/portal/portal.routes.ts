import { Routes } from '@angular/router';

export const portalRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./portal-layout/portal-layout').then(m => m.PortalLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./dashboard/dashboard').then(m => m.DashboardComponent) },
      { path: 'products', loadComponent: () => import('./products/product-list/product-list').then(m => m.ProductListComponent) },
      { path: 'products/:id', loadComponent: () => import('./products/product-detail/product-detail').then(m => m.ProductDetailComponent) },
      { path: 'quote', loadComponent: () => import('./quote/quote').then(m => m.QuoteComponent) },
      { path: 'quote/:productId', loadComponent: () => import('./quote/quote').then(m => m.QuoteComponent) },
      { path: 'proposals', loadComponent: () => import('./proposals/proposal-list/proposal-list').then(m => m.ProposalListComponent) },
      { path: 'proposals/new', loadComponent: () => import('./proposals/proposal-submit/proposal-submit').then(m => m.ProposalSubmitComponent) },
      { path: 'proposals/:id', loadComponent: () => import('./proposals/proposal-detail/proposal-detail').then(m => m.ProposalDetailComponent) },
      { path: 'policies', loadComponent: () => import('./policies/policy-list/policy-list').then(m => m.PolicyListComponent) },
      { path: 'policies/:id', loadComponent: () => import('./policies/policy-detail/policy-detail').then(m => m.PolicyDetailComponent) },
      { path: 'claims', loadComponent: () => import('./claims/claim-list/claim-list').then(m => m.ClaimListComponent) },
      { path: 'claims/new', loadComponent: () => import('./claims/claim-file/claim-file').then(m => m.ClaimFileComponent) },
      { path: 'claims/:id', loadComponent: () => import('./claims/claim-detail/claim-detail').then(m => m.ClaimDetailComponent) },
      { path: 'pay/:policyId', loadComponent: () => import('./payments/pay-premium/pay-premium').then(m => m.PayPremiumComponent) },
      { path: 'payments', loadComponent: () => import('./payments/payment-history/payment-history').then(m => m.PaymentHistoryComponent) },
      { path: 'notifications', loadComponent: () => import('./notifications/notification-list').then(m => m.NotificationListComponent) },
      { path: 'grievances', loadComponent: () => import('./grievances/grievance-list/grievance-list').then(m => m.GrievanceListComponent) },
      { path: 'grievances/new', loadComponent: () => import('./grievances/grievance-raise/grievance-raise').then(m => m.GrievanceRaiseComponent) },
      { path: 'grievances/:id', loadComponent: () => import('./grievances/grievance-detail/grievance-detail').then(m => m.GrievanceDetailComponent) },
      { path: 'family', loadComponent: () => import('./family/family').then(m => m.FamilyComponent) },
      { path: 'kyc', loadComponent: () => import('./kyc/kyc').then(m => m.KycComponent) },
      { path: 'profile', loadComponent: () => import('./profile/profile').then(m => m.ProfileComponent) },
    ],
  },
];
