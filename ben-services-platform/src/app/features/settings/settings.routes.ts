import { Routes } from '@angular/router';
import { AdminManagementPageComponent } from './pages/admin-management-page.component';
import { SettingsPageComponent } from './pages/settings-page.component';

export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    component: SettingsPageComponent
  },
  {
    path: 'admins',
    component: AdminManagementPageComponent
  }
];
