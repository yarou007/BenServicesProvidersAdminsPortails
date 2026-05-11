import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { AdminRole, AdminUser } from '../../../core/models/admin.model';
import { AdminService } from '../../../core/services/admin.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-management-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  templateUrl: './admin-management-page.component.html',
  styleUrl: './admin-management-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminManagementPageComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly admins = signal<AdminUser[]>([]);
  protected readonly loading = signal(false);
  protected readonly creating = signal(false);
  protected readonly busyAdminIds = signal<number[]>([]);

  protected readonly roles: AdminRole[] = ['ADMIN', 'SUPER_ADMIN', 'STAFF', 'PROVIDER'];

  protected readonly createForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    role: ['ADMIN' as AdminRole, [Validators.required]]
  });

  protected readonly currentAdminId = computed(() => this.authService.getCurrentUser()?.id ?? null);

  ngOnInit(): void {
    this.loadAdmins();
  }

  protected loadAdmins(): void {
    this.loading.set(true);

    this.adminService.listAdmins().subscribe({
      next: (admins) => {
        this.loading.set(false);
        this.admins.set(admins);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.snackBar.open(this.resolveErrorMessage(error), 'Close', {
          duration: 3500
        });
      }
    });
  }

  protected createAdmin(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.creating.set(true);

    const { fullName, email, role } = this.createForm.getRawValue();

    this.adminService
      .createAdmin({
        fullName,
        email,
        role
      })
      .pipe(
        finalize(() => {
          this.creating.set(false);
        })
      )
      .subscribe({
        next: (response) => {
          this.loadAdmins();
          this.createForm.reset({
            fullName: '',
            email: '',
            role: 'ADMIN'
          });

          const notificationMessage = response.emailSent
            ? 'Admin created and credentials email sent.'
            : 'Admin created, but credentials email could not be sent.';

          this.snackBar.open(notificationMessage, 'Close', {
            duration: 3500
          });
        },
        error: (error: unknown) => {
          this.snackBar.open(this.resolveErrorMessage(error), 'Close', {
            duration: 3500
          });
        }
      });
  }

  protected toggleStatus(admin: AdminUser): void {
    const nextStatus = !admin.isActive;
    this.setBusy(admin.id, true);

    this.adminService.updateAdminStatus(admin.id, nextStatus).subscribe({
      next: (updatedAdmin) => {
        this.setBusy(admin.id, false);
        this.admins.set(this.admins().map((item) => (item.id === updatedAdmin.id ? updatedAdmin : item)));
      },
      error: (error: unknown) => {
        this.setBusy(admin.id, false);
        this.snackBar.open(this.resolveErrorMessage(error), 'Close', {
          duration: 3500
        });
      }
    });
  }

  protected isBusy(adminId: number): boolean {
    return this.busyAdminIds().includes(adminId);
  }

  protected isSelf(adminId: number): boolean {
    return this.currentAdminId() === adminId;
  }

  private setBusy(adminId: number, busy: boolean): void {
    if (busy) {
      this.busyAdminIds.set([...this.busyAdminIds(), adminId]);
      return;
    }

    this.busyAdminIds.set(this.busyAdminIds().filter((id) => id !== adminId));
  }

  private resolveErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.message === 'string') {
      return error.error.message;
    }

    return 'An unexpected error occurred.';
  }
}
