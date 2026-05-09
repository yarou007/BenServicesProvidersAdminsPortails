import { Injectable, computed, signal } from '@angular/core';

const AUTH_STORAGE_KEY = 'ben_services_platform_auth';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly loggedIn = signal(false);

  readonly isLoggedIn = computed(() => this.loggedIn());

  constructor() {
    const isPersisted = this.safeReadStorage();
    this.loggedIn.set(isPersisted);
  }

  login(email: string, password: string, rememberMe: boolean): boolean {
    if (!email || !password) {
      return false;
    }

    this.loggedIn.set(true);

    if (rememberMe) {
      localStorage.setItem(AUTH_STORAGE_KEY, 'true');
    } else {
      localStorage.removeItem(AUTH_STORAGE_KEY);
    }

    return true;
  }

  logout(): void {
    this.loggedIn.set(false);
    localStorage.removeItem(AUTH_STORAGE_KEY);
  }

  isAuthenticated(): boolean {
    return this.loggedIn();
  }

  private safeReadStorage(): boolean {
    try {
      return localStorage.getItem(AUTH_STORAGE_KEY) === 'true';
    } catch {
      return false;
    }
  }
}
