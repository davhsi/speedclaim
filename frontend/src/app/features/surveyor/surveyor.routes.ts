import { Routes } from '@angular/router';

export const surveyorRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./surveyor-layout/surveyor-layout').then(m => m.SurveyorLayoutComponent),
    children: [
      { path: '', redirectTo: 'claims', pathMatch: 'full' },
      { path: 'claims', loadComponent: () => import('./surveyor-claims/surveyor-claims').then(m => m.SurveyorClaimsComponent) },
      { path: 'claims/:id/report', loadComponent: () => import('./survey-report/survey-report').then(m => m.SurveyReportComponent) },
      { path: 'history', loadComponent: () => import('./surveyor-history/surveyor-history').then(m => m.SurveyorHistoryComponent) },
      { path: 'profile', loadComponent: () => import('./surveyor-profile/surveyor-profile').then(m => m.SurveyorProfileComponent) },
    ],
  },
];
