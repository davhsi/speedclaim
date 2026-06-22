import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { agentGuard } from './core/guards/agent.guard';
import { claimsOfficerGuard } from './core/guards/claims-officer.guard';
import { financeOfficerGuard } from './core/guards/finance-officer.guard';
import { surveyorGuard } from './core/guards/surveyor.guard';
import { underwriterGuard } from './core/guards/underwriter.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes),
  },
  {
    path: 'agent',
    canActivate: [authGuard, agentGuard],
    loadChildren: () => import('./features/agent/agent.routes').then(m => m.agentRoutes),
  },
  {
    path: 'claims-officer',
    canActivate: [authGuard, claimsOfficerGuard],
    loadChildren: () => import('./features/claims-officer/claims-officer.routes').then(m => m.claimsOfficerRoutes),
  },
  {
    path: 'finance-officer',
    canActivate: [authGuard, financeOfficerGuard],
    loadChildren: () => import('./features/finance-officer/finance-officer.routes').then(m => m.financeOfficerRoutes),
  },
  {
    path: 'underwriter',
    canActivate: [authGuard, underwriterGuard],
    loadChildren: () => import('./features/underwriter/underwriter.routes').then(m => m.underwriterRoutes),
  },
  {
    path: 'surveyor',
    canActivate: [authGuard, surveyorGuard],
    loadChildren: () => import('./features/surveyor/surveyor.routes').then(m => m.surveyorRoutes),
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadChildren: () => import('./features/portal/portal.routes').then(m => m.portalRoutes),
  },
  { path: '**', redirectTo: '' },
];
