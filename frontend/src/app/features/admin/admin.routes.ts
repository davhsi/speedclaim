import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./admin-layout/admin-layout').then(m => m.AdminLayoutComponent),
    children: [
      { path: '', redirectTo: 'users', pathMatch: 'full' },
      { path: 'users', loadComponent: () => import('./admin-users/admin-users').then(m => m.AdminUsersComponent) },
      { path: 'agents', loadComponent: () => import('./admin-agents/admin-agents').then(m => m.AdminAgentsComponent) },
      { path: 'products', loadComponent: () => import('./admin-products/admin-products').then(m => m.AdminProductsComponent) },
      { path: 'brochures', loadComponent: () => import('./admin-brochures/admin-brochures').then(m => m.AdminBrochuresComponent) },
      { path: 'system', loadComponent: () => import('./admin-system/admin-system').then(m => m.AdminSystemComponent) },
    ],
  },
];
