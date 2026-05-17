import { Routes } from '@angular/router';
import { adminChildGuard, adminGuard, anonymousOnlyMatchGuard, authenticatedGuard } from './core/guards/admin.guard';
import { MainLayoutComponent } from './core/layout/main-layout.component';

export const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES)
  },
  {
    path: 'change-password',
    canActivate: [authenticatedGuard],
    loadComponent: () =>
      import('./features/auth/change-password/change-password-page.component').then(
        (m) => m.ChangePasswordPageComponent
      )
  },
  {
    path: '',
    canMatch: [anonymousOnlyMatchGuard],
    loadChildren: () => import('./features/public/public.routes').then((m) => m.PUBLIC_ROUTES)
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [adminGuard],
    canActivateChild: [adminChildGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES)
      },
      {
        path: 'providers',
        loadChildren: () => import('./features/providers/providers.routes').then((m) => m.PROVIDERS_ROUTES)
      },
      {
        path: 'applications',
        loadChildren: () => import('./features/applications/applications.routes').then((m) => m.APPLICATIONS_ROUTES)
      },
      {
        path: 'regions',
        loadChildren: () => import('./features/regions/regions.routes').then((m) => m.REGIONS_ROUTES)
      },
      {
        path: 'reports',
        loadChildren: () => import('./features/reports/reports.routes').then((m) => m.REPORTS_ROUTES)
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.routes').then((m) => m.SETTINGS_ROUTES)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
