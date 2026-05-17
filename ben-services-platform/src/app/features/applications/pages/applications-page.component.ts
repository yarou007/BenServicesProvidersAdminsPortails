import { BreakpointObserver } from '@angular/cdk/layout';
import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, OnInit, inject } from '@angular/core';
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
      <p><strong>Street Address:</strong> {{ data.streetAddress || 'N/A' }}</p>
      <p><strong>Email:</strong> {{ data.email }}</p>
      <p><strong>Phone:</strong> {{ data.phone }}</p>
      <p><strong>Service Type:</strong> {{ data.serviceType }}</p>
      <p><strong>Services:</strong> {{ data.servicesOffered.join(', ') }}</p>
      <p><strong>States Covered:</strong> {{ (data.states ?? []).length ? (data.states ?? []).join(', ') : (data.state || 'N/A') }}</p>
      <p><strong>Cities Covered:</strong> {{ data.citiesCovered.join(', ') }}</p>
      <p><strong>State:</strong> {{ data.state }}</p>
      <p><strong>ZIP Codes:</strong> {{ data.zipCodes.join(', ') }}</p>
      <p><strong>Years of Experience:</strong> {{ data.yearsOfExperience }}</p>
      <p><strong>Emergency Service:</strong> {{ data.emergencyService ? 'Yes' : 'No' }}</p>
      <p><strong>Working Hours:</strong> {{ data.workingHours }}</p>
      <p><strong>Message:</strong> {{ data.message || 'N/A' }}</p>

      <p><strong>Status:</strong> {{ data.status }}</p>
      <p><strong>Admin Notes:</strong> {{ data.adminNotes || 'N/A' }}</p>
      <p><strong>Missing Info Reason:</strong> {{ data.missingInfoReason || 'N/A' }}</p>
      <p><strong>Rejection Reason:</strong> {{ data.rejectionReason || 'N/A' }}</p>
      <p><strong>Verification Notes:</strong> {{ data.verificationNotes || 'N/A' }}</p>
      <p><strong>Converted Provider ID:</strong> {{ data.convertedProviderId || 'N/A' }}</p>

      <p><strong>Submitted:</strong> {{ data.submittedAt | date: 'medium' }}</p>
      <p><strong>Reviewed:</strong> {{ data.reviewedAt ? (data.reviewedAt | date: 'medium') : 'N/A' }}</p>
      <p><strong>Verified:</strong> {{ data.verifiedAt ? (data.verifiedAt | date: 'medium') : 'N/A' }}</p>
      <p><strong>Rejected:</strong> {{ data.rejectedAt ? (data.rejectedAt | date: 'medium') : 'N/A' }}</p>

      <div class="doc-links">
        <strong>Documents:</strong>
        <div class="links">
          <button *ngIf="data.licenseFileUrl" mat-stroked-button (click)="downloadDocument('license')">License</button>
          <button *ngIf="data.insuranceFileUrl" mat-stroked-button (click)="downloadDocument('insurance')">
            Insurance / COI
          </button>
          <button *ngIf="data.w9FileUrl" mat-stroked-button (click)="downloadDocument('w9')">W-9</button>
          <span *ngIf="!data.licenseFileUrl && !data.insuranceFileUrl && !data.w9FileUrl">No documents uploaded.</span>
        </div>
      </div>
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
        min-width: min(720px, 92vw);
      }

      p {
        margin: 0;
      }

      .doc-links {
        margin-top: 0.3rem;
      }

      .links {
        display: flex;
        gap: 0.55rem;
        flex-wrap: wrap;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ApplicationDetailDialogComponent {
  private readonly applicationService = inject(ApplicationService);
  private readonly snackBar = inject(MatSnackBar);

  constructor(@Inject(MAT_DIALOG_DATA) protected readonly data: ProviderApplication) {}

  protected downloadDocument(documentType: 'license' | 'insurance' | 'w9'): void {
    this.applicationService.downloadApplicationDocument(this.data.id, documentType).subscribe({
      next: (fileBlob) => {
        const blobUrl = window.URL.createObjectURL(fileBlob);
        const anchor = document.createElement('a');
        anchor.href = blobUrl;
        anchor.download = `${documentType}-${this.data.businessName || this.data.fullName}`.replace(/\s+/g, '-');
        anchor.click();
        window.URL.revokeObjectURL(blobUrl);
      },
      error: () => {
        this.snackBar.open('Unable to download this document.', 'Close', { duration: 2400 });
      }
    });
  }
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
export class ApplicationsPageComponent implements OnInit {
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
    'fullName',
    'businessName',
    'phone',
    'email',
    'serviceType',
    'city',
    'state',
    'submittedAt',
    'status',
    'actions'
  ];

  ngOnInit(): void {
    this.applicationService.refreshApplications().subscribe();
  }

  protected viewApplication(application: ProviderApplication): void {
    this.dialog.open(ApplicationDetailDialogComponent, {
      data: application,
      maxWidth: '880px'
    });
  }

  protected markUnderReview(application: ProviderApplication): void {
    this.applicationService.markUnderReview(application.id).subscribe({
      next: () => this.snackBar.open('Application moved to UnderReview.', 'Close', { duration: 2300 }),
      error: () => this.snackBar.open('Unable to mark UnderReview.', 'Close', { duration: 2600 })
    });
  }

  protected requestMissingInfo(application: ProviderApplication): void {
    const reason = window.prompt('Enter missing information reason (required):', application.missingInfoReason ?? '');
    if (reason === null) {
      return;
    }

    if (!reason.trim()) {
      this.snackBar.open('Missing info reason is required.', 'Close', { duration: 2400 });
      return;
    }

    this.applicationService.requestMissingInfo(application.id, reason).subscribe({
      next: () => this.snackBar.open('Application marked MissingInfo and email sent.', 'Close', { duration: 2400 }),
      error: () => this.snackBar.open('Unable to request missing info.', 'Close', { duration: 2600 })
    });
  }

  protected rejectApplication(application: ProviderApplication): void {
    const reason = window.prompt('Enter rejection reason (required):', application.rejectionReason ?? '');
    if (reason === null) {
      return;
    }

    if (!reason.trim()) {
      this.snackBar.open('Rejection reason is required.', 'Close', { duration: 2400 });
      return;
    }

    this.applicationService.rejectApplication(application.id, reason).subscribe({
      next: () => this.snackBar.open('Application rejected and email sent.', 'Close', { duration: 2400 }),
      error: () => this.snackBar.open('Reject action failed.', 'Close', { duration: 2600 })
    });
  }

  protected acceptApplication(application: ProviderApplication): void {
    this.applicationService.acceptApplication(application.id).subscribe({
      next: () => this.snackBar.open('Application accepted.', 'Close', { duration: 2200 }),
      error: () => this.snackBar.open('Accept action failed.', 'Close', { duration: 2600 })
    });
  }

  protected verifyApplication(application: ProviderApplication): void {
    const notes = window.prompt('Optional verification notes:', application.verificationNotes ?? '');
    if (notes === null) {
      return;
    }

    this.applicationService.verifyApplication(application.id, notes.trim() || undefined).subscribe({
      next: () => this.snackBar.open('Provider verified, activated, and credentials emailed.', 'Close', { duration: 2600 }),
      error: () => this.snackBar.open('Verify action failed.', 'Close', { duration: 2600 })
    });
  }

  protected convertToProvider(application: ProviderApplication): void {
    this.applicationService.convertToProvider(application.id).subscribe({
      next: () => this.snackBar.open('Application converted to Provider.', 'Close', { duration: 2400 }),
      error: () => this.snackBar.open('Convert action failed.', 'Close', { duration: 2600 })
    });
  }

  protected editNotes(application: ProviderApplication): void {
    const nextNotes = window.prompt('Add or update admin notes for this application:', application.adminNotes ?? '');
    if (nextNotes === null) {
      return;
    }

    this.applicationService.updateApplicationNotes(application.id, nextNotes).subscribe({
      next: () => this.snackBar.open('Admin notes saved.', 'Close', { duration: 2000 }),
      error: () => this.snackBar.open('Unable to save notes.', 'Close', { duration: 2400 })
    });
  }

  protected canMarkUnderReview(application: ProviderApplication): boolean {
    return application.status === 'Pending' || application.status === 'MissingInfo';
  }

  protected canRequestMissingInfo(application: ProviderApplication): boolean {
    return application.status === 'Pending' || application.status === 'UnderReview' || application.status === 'Accepted';
  }

  protected canReject(application: ProviderApplication): boolean {
    return application.status !== 'Converted' && application.status !== 'Rejected';
  }

  protected canAccept(application: ProviderApplication): boolean {
    return application.status === 'Pending' || application.status === 'UnderReview' || application.status === 'MissingInfo';
  }

  protected canVerify(application: ProviderApplication): boolean {
    return application.status === 'Accepted';
  }

  protected canConvert(application: ProviderApplication): boolean {
    return application.status === 'Verified' || application.status === 'Accepted';
  }

  protected hasAnyDocument(application: ProviderApplication): boolean {
    return Boolean(application.licenseFileUrl || application.insuranceFileUrl || application.w9FileUrl);
  }
}
