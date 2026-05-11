export type AdminRole = 'SUPER_ADMIN' | 'ADMIN' | 'STAFF' | 'PROVIDER';

export interface AdminUser {
  id: number;
  fullName: string;
  email: string;
  username: string;
  role: AdminRole;
  isActive: boolean;
  mustChangePassword: boolean;
  createdAt?: string;
  updatedAt?: string;
  createdByAdminId?: number | null;
}
