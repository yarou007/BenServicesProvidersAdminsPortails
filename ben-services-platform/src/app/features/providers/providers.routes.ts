import { Routes } from '@angular/router';
import { ProviderDetailsPageComponent } from './pages/provider-details-page.component';
import { ProviderFormPageComponent } from './pages/provider-form-page.component';
import { ProvidersListPageComponent } from './pages/providers-list-page.component';

export const PROVIDERS_ROUTES: Routes = [
  {
    path: '',
    component: ProvidersListPageComponent
  },
  {
    path: 'add',
    component: ProviderFormPageComponent
  },
  {
    path: ':id/edit',
    component: ProviderFormPageComponent
  },
  {
    path: ':id',
    component: ProviderDetailsPageComponent
  }
];
