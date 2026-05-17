import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

const evaluateAuthenticatedAccess = (url: string, authService: AuthService, router: Router) => {
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
    return authService.isAdminRole() ? router.createUrlTree(['/dashboard']) : router.createUrlTree(['/']);
  }

  return true;
};

export const authenticatedGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return evaluateAuthenticatedAccess(state.url, authService, router);
};

export const adminGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const authCheck = evaluateAuthenticatedAccess(state.url, authService, router);
  if (authCheck !== true) {
    return authCheck;
  }

  return authService.isAdminRole() ? true : router.createUrlTree(['/']);
};

export const publicGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  return authService.mustChangePassword()
    ? router.createUrlTree(['/change-password'])
    : authService.isAdminRole()
      ? router.createUrlTree(['/dashboard'])
      : router.createUrlTree(['/']);
};

export const adminChildGuard: CanActivateChildFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const authCheck = evaluateAuthenticatedAccess(state.url, authService, router);
  if (authCheck !== true) {
    return authCheck;
  }

  return authService.isAdminRole() ? true : router.createUrlTree(['/']);
};

export const anonymousOnlyMatchGuard: CanMatchFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  if (authService.mustChangePassword()) {
    return router.createUrlTree(['/change-password']);
  }

  return !authService.isAdminRole();
};
