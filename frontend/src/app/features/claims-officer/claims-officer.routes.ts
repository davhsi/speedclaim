import { Routes } from '@angular/router';

export const claimsOfficerRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./claims-officer-layout/claims-officer-layout').then(m => m.ClaimsOfficerLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./claims-officer-dashboard/claims-officer-dashboard').then(m => m.ClaimsOfficerDashboardComponent) },
      { path: 'claims', loadComponent: () => import('./claims-officer-claims/claim-list').then(m => m.ClaimListComponent) },
      { path: 'claims/:id', loadComponent: () => import('./claims-officer-claims/claim-detail').then(m => m.ClaimDetailComponent) },
      { path: 'grievances', loadComponent: () => import('./claims-officer-grievances/grievance-list').then(m => m.GrievanceListComponent) },
      { path: 'grievances/:id', loadComponent: () => import('./claims-officer-grievances/grievance-detail').then(m => m.GrievanceDetailComponent) },
      { path: 'profile', loadComponent: () => import('./claims-officer-profile/claims-officer-profile').then(m => m.ClaimsOfficerProfileComponent) },
    ],
  },
];
