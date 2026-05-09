import { BreakpointObserver } from '@angular/cdk/layout';
import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { map } from 'rxjs';
import { StatusBadgeComponent } from '../../../shared/components/status-badge.component';
import { ProviderApplication } from '../../../shared/models/application.model';
import { ApplicationService } from '../../../shared/services/application.service';

@Component({
  selector: 'app-application-detail-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Application Details</h2>
    <mat-dialog-content class="dialog-content">
      <p><strong>Applicant:</strong> {{ data.fullName }}</p>
      <p><strong>Company:</strong> {{ data.businessName }}</p>
      <p><strong>Email:</strong> {{ data.email }}</p>
      <p><strong>Phone:</strong> {{ data.phone }}</p>
      <p><strong>Service Type:</strong> {{ data.serviceType }}</p>
      <p><strong>Services:</strong> {{ data.servicesOffered.join(', ') }}</p>
      <p><strong>Cities Covered:</strong> {{ data.citiesCovered.join(', ') }}</p>
      <p><strong>State:</strong> {{ data.state }}</p>
      <p><strong>ZIP Codes:</strong> {{ data.zipCodes.join(', ') }}</p>
      <p><strong>Years of Experience:</strong> {{ data.yearsOfExperience }}</p>
      <p><strong>Emergency Service:</strong> {{ data.emergencyService ? 'Yes' : 'No' }}</p>
      <p><strong>Working Hours:</strong> {{ data.workingHours }}</p>
      <p><strong>Notes:</strong> {{ data.message || 'N/A' }}</p>
      <p><strong>License File:</strong> {{ data.licenseFileName || 'Not uploaded' }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .dialog-content {
        display: grid;
        gap: 0.45rem;
        min-width: min(620px, 90vw);
      }

      p {
        margin: 0;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ApplicationDetailDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) protected readonly data: ProviderApplication) {}
}

@Component({
  selector: 'app-applications-page',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatSnackBarModule,
    StatusBadgeComponent
  ],
  templateUrl: './applications-page.component.html',
  styleUrl: './applications-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ApplicationsPageComponent {
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly applicationService = inject(ApplicationService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  protected readonly applications$ = this.applicationService.applications$;
  protected readonly isCompact = toSignal(
    this.breakpointObserver.observe('(max-width: 980px)').pipe(map((state) => state.matches)),
    { initialValue: false }
  );

  protected readonly displayedColumns = [
    'applicantName',
    'company',
    'serviceType',
    'city',
    'state',
    'dateSubmitted',
    'status',
    'actions'
  ];

  protected viewApplication(application: ProviderApplication): void {
    this.dialog.open(ApplicationDetailDialogComponent, {
      data: application,
      maxWidth: '760px'
    });
  }

  protected approveApplication(id: number): void {
    this.applicationService.approveApplication(id).subscribe({
      next: () => this.snackBar.open('Application approved and added to providers.', 'Close', { duration: 1800 }),
      error: () => this.snackBar.open('Approval failed. Please retry.', 'Close', { duration: 2200 })
    });
  }

  protected rejectApplication(id: number): void {
    this.applicationService.rejectApplication(id).subscribe({
      next: () => this.snackBar.open('Application rejected.', 'Close', { duration: 1800 }),
      error: () => this.snackBar.open('Reject action failed.', 'Close', { duration: 2200 })
    });
  }

  protected requestMoreInfo(id: number): void {
    this.applicationService.requestMoreInfo(id).subscribe({
      next: () => this.snackBar.open('Requested more info from applicant.', 'Close', { duration: 1800 }),
      error: () => this.snackBar.open('Request action failed.', 'Close', { duration: 2200 })
    });
  }
}
