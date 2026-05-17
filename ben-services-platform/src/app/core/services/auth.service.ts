import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { AdminUser } from '../models/admin.model';

interface LoginResponse {
  token: string;
  admin: AdminUser;
}

interface ChangePasswordResponse {
  message: string;
  admin: AdminUser;
}

const AUTH_TOKEN_STORAGE_KEY = 'ben_services_platform_auth_token';
const AUTH_ADMIN_STORAGE_KEY = 'ben_services_platform_auth_admin';
const ADMIN_ROLES = new Set(['SUPER_ADMIN', 'ADMIN', 'STAFF']);

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly httpClient = inject(HttpClient);

  private readonly tokenSignal = signal<string | null>(null);
  private readonly adminSignal = signal<AdminUser | null>(null);

  readonly isLoggedIn = computed(() => !!this.tokenSignal());
  readonly currentAdmin = computed(() => this.adminSignal());

  constructor() {
    this.restoreSession();
  }

  login(login: string, password: string): Observable<AdminUser> {
    return this.httpClient.post<LoginResponse>(`${API_BASE_URL}/auth/login`, { login, password }).pipe(
      tap((response) => this.setSession(response.token, response.admin)),
      map((response) => response.admin)
    );
  }

  refreshCurrentUser(): Observable<AdminUser> {
    return this.httpClient.get<AdminUser>(`${API_BASE_URL}/auth/me`).pipe(tap((admin) => this.setCurrentAdmin(admin)));
  }

  changePassword(currentPassword: string, newPassword: string, confirmPassword: string): Observable<AdminUser> {
    return this.httpClient
      .post<ChangePasswordResponse>(`${API_BASE_URL}/auth/change-password`, {
        currentPassword,
        newPassword,
        confirmPassword
      })
      .pipe(
        tap((response) => this.setCurrentAdmin(response.admin)),
        map((response) => response.admin)
      );
  }

  logout(): void {
    this.clearSession();
  }

  getCurrentUser(): AdminUser | null {
    return this.adminSignal();
  }

  isAuthenticated(): boolean {
    return !!this.tokenSignal();
  }

  getToken(): string | null {
    return this.tokenSignal();
  }

  hasRole(role: string): boolean {
    return this.adminSignal()?.role === role;
  }

  getRole(): string | null {
    return this.adminSignal()?.role ?? null;
  }

  isAdminRole(): boolean {
    const role = this.getRole();
    return role ? ADMIN_ROLES.has(role) : false;
  }

  mustChangePassword(): boolean {
    return this.adminSignal()?.mustChangePassword ?? false;
  }

  clearSession(): void {
    this.tokenSignal.set(null);
    this.adminSignal.set(null);

    this.safeStorageWrite(() => {
      localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
      localStorage.removeItem(AUTH_ADMIN_STORAGE_KEY);
    });
  }

  private setSession(token: string, admin: AdminUser): void {
    this.tokenSignal.set(token);
    this.adminSignal.set(admin);

    this.safeStorageWrite(() => {
      localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token);
      localStorage.setItem(AUTH_ADMIN_STORAGE_KEY, JSON.stringify(admin));
    });
  }

  private setCurrentAdmin(admin: AdminUser): void {
    this.adminSignal.set(admin);

    this.safeStorageWrite(() => {
      localStorage.setItem(AUTH_ADMIN_STORAGE_KEY, JSON.stringify(admin));
    });
  }

  private restoreSession(): void {
    this.safeStorageRead(() => {
      const token = localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
      const adminRaw = localStorage.getItem(AUTH_ADMIN_STORAGE_KEY);

      if (!token || !adminRaw) {
        return;
      }

      const admin = JSON.parse(adminRaw) as AdminUser;
      this.tokenSignal.set(token);
      this.adminSignal.set(admin);
    });
  }

  private safeStorageRead(callback: () => void): void {
    try {
      callback();
    } catch {
      this.tokenSignal.set(null);
      this.adminSignal.set(null);
    }
  }

  private safeStorageWrite(callback: () => void): void {
    try {
      callback();
    } catch {
      // Ignore storage errors (private mode/quota), auth still works in-memory.
    }
  }
}
