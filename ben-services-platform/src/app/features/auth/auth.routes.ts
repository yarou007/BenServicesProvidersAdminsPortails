import { Routes } from '@angular/router';
import { publicGuard } from '../../core/guards/admin.guard';
import { LoginPageComponent } from './login/login-page.component';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    canActivate: [publicGuard],
    component: LoginPageComponent
  }
];
