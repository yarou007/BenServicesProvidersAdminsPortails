import { Routes } from '@angular/router';
import { CommercialLandingPageComponent } from './pages/commercial-landing-page.component';
import { CommercialRequestFormPageComponent } from './pages/commercial-request-form-page.component';
import { ProviderApplicationFormPageComponent } from './pages/provider-application-form-page.component';
import { ProviderLandingPageComponent } from './pages/provider-landing-page.component';
import { ProviderPendingPageComponent } from './pages/provider-pending-page.component';
import { PublicHomePageComponent } from './pages/public-home-page.component';
import { PublicLayoutPageComponent } from './pages/public-layout-page.component';
import { ResidentialPlaceholderPageComponent } from './pages/residential-placeholder-page.component';

export const PUBLIC_ROUTES: Routes = [
  {
    path: '',
    component: PublicLayoutPageComponent,
    children: [
      {
        path: '',
        pathMatch: 'full',
        component: PublicHomePageComponent
      },
      {
        path: 'providers',
        component: ProviderLandingPageComponent
      },
      {
        path: 'apply',
        component: ProviderApplicationFormPageComponent
      },
      {
        path: 'apply-as-provider',
        pathMatch: 'full',
        redirectTo: 'apply'
      },
      {
        path: 'provider/pending',
        component: ProviderPendingPageComponent
      },
      {
        path: 'commercial',
        component: CommercialLandingPageComponent
      },
      {
        path: 'request-commercial-service',
        component: CommercialRequestFormPageComponent
      },
      {
        path: 'residential',
        component: ResidentialPlaceholderPageComponent
      }
    ]
  }
];
