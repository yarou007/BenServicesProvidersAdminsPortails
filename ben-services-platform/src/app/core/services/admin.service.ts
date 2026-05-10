import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { AdminRole, AdminUser } from '../models/admin.model';

export interface CreateAdminRequest {
  fullName: string;
  email: string;
  role: AdminRole;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly httpClient = inject(HttpClient);

  listAdmins(): Observable<AdminUser[]> {
    return this.httpClient.get<AdminUser[]>(`${API_BASE_URL}/admins`);
  }

  getAdminById(id: number): Observable<AdminUser> {
    return this.httpClient.get<AdminUser>(`${API_BASE_URL}/admins/${id}`);
  }

  createAdmin(request: CreateAdminRequest): Observable<AdminUser> {
    return this.httpClient.post<AdminUser>(`${API_BASE_URL}/admins`, request);
  }

  updateAdminStatus(id: number, isActive: boolean): Observable<AdminUser> {
    return this.httpClient.patch<AdminUser>(`${API_BASE_URL}/admins/${id}/status`, { isActive });
  }
}
