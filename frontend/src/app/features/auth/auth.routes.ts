import { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';

export const authRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./auth-layout/auth-layout').then(m => m.AuthLayoutComponent),
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login', loadComponent: () => import('./login/login').then(m => m.LoginComponent) },
      {
        path: 'register',
        loadComponent: () => import('./register/register').then(m => m.RegisterComponent),
        canDeactivate: [unsavedChangesGuard],
      },
      { path: 'forgot-password', loadComponent: () => import('./forgot-password/forgot-password').then(m => m.ForgotPasswordComponent) },
      { path: 'reset-password', loadComponent: () => import('./reset-password/reset-password').then(m => m.ResetPasswordComponent) },
      { path: 'reset-sent', loadComponent: () => import('./reset-sent/reset-sent').then(m => m.ResetSentComponent) },
      { path: 'verify-email', loadComponent: () => import('./verify-email/verify-email').then(m => m.VerifyEmailComponent) },
    ],
  },
];
