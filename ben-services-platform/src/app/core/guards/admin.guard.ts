import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

const evaluateAccess = (url: string, authService: AuthService, router: Router) => {
  const normalizedUrl = url.split('?')[0];

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  const forcePasswordChange = authService.mustChangePassword();
  const isOnChangePasswordPage = normalizedUrl.startsWith('/change-password');

  if (forcePasswordChange && !isOnChangePasswordPage) {
    return router.createUrlTree(['/change-password']);
  }

  if (!forcePasswordChange && isOnChangePasswordPage) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};

export const adminGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return evaluateAccess(state.url, authService, router);
};

export const publicGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  return authService.mustChangePassword()
    ? router.createUrlTree(['/change-password'])
    : router.createUrlTree(['/dashboard']);
};

export const adminChildGuard: CanActivateChildFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return evaluateAccess(state.url, authService, router);
};
