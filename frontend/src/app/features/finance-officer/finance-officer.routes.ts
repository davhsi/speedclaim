import { Routes } from '@angular/router';

export const financeOfficerRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./finance-officer-layout/finance-officer-layout').then(m => m.FinanceOfficerLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./finance-officer-dashboard/finance-officer-dashboard').then(m => m.FinanceOfficerDashboardComponent) },
      { path: 'payments', loadComponent: () => import('./finance-officer-payments/finance-officer-payments').then(m => m.FinanceOfficerPaymentsComponent) },
      { path: 'payouts', loadComponent: () => import('./finance-officer-payouts/finance-officer-payouts').then(m => m.FinanceOfficerPayoutsComponent) },
      { path: 'commissions', loadComponent: () => import('./finance-officer-commissions/finance-officer-commissions').then(m => m.FinanceOfficerCommissionsComponent) },
      { path: 'reports', loadComponent: () => import('./finance-officer-reports/finance-officer-reports').then(m => m.FinanceOfficerReportsComponent) },
      { path: 'profile', loadComponent: () => import('./finance-officer-profile/finance-officer-profile').then(m => m.FinanceOfficerProfileComponent) },
    ],
  },
];
