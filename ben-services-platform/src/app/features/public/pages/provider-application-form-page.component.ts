import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { SERVICE_AREAS_BY_STATE, SERVICE_AREA_STATES, ServiceAreaState } from '../../../shared/data/service-areas';
import { SERVICE_OPTIONS } from '../../../shared/services/mock-data';
import { ApplicationService } from '../../../shared/services/application.service';

const ALLOWED_FILE_EXTENSIONS = new Set(['pdf', 'jpg', 'jpeg', 'png']);

@Component({
  selector: 'app-provider-application-form-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './provider-application-form-page.component.html',
  styleUrl: './provider-application-form-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProviderApplicationFormPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly applicationService = inject(ApplicationService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly serviceOptions = SERVICE_OPTIONS;
  protected readonly stateOptions = SERVICE_AREA_STATES;
  protected cityOptions: string[] = [];

  protected readonly applicationForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required]],
    businessName: ['', [Validators.required]],
    phone: ['', [Validators.required, Validators.pattern(/^\(?\d{3}\)?[-\s]?\d{3}[-\s]?\d{4}$/)]],
    email: ['', [Validators.required, Validators.email]],
    serviceType: ['Locksmith', [Validators.required]],
    servicesOffered: [[] as string[], [Validators.required]],
    state: ['', [Validators.required]],
    citiesCovered: [[] as string[], [Validators.required]],
    zipCodes: ['', [Validators.required]],
    yearsOfExperience: [1, [Validators.required, Validators.min(0)]],
    emergencyService: [false],
    workingHours: ['', [Validators.required]],
    message: ['']
  });

  protected isSubmitting = false;
  protected submissionError = '';

  protected selectedLicenseFileName = '';
  protected selectedInsuranceFileName = '';
  protected selectedW9FileName = '';

  private selectedLicenseFile: File | null = null;
  private selectedInsuranceFile: File | null = null;
  private selectedW9File: File | null = null;

  constructor() {
    this.applicationForm.controls.state.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((state) => {
      const normalizedState = state as ServiceAreaState | '';

      this.cityOptions = normalizedState ? [...(SERVICE_AREAS_BY_STATE[normalizedState] ?? [])] : [];
      this.applicationForm.controls.citiesCovered.setValue([]);
      this.applicationForm.controls.citiesCovered.markAsUntouched();
    });
  }

  protected onFileSelected(event: Event, documentType: 'license' | 'insurance' | 'w9'): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (!file) {
      this.assignFile(documentType, null);
      return;
    }

    const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (!ALLOWED_FILE_EXTENSIONS.has(extension)) {
      this.submissionError = 'Only PDF, JPG, JPEG, and PNG files are allowed.';
      this.assignFile(documentType, null);
      input.value = '';
      return;
    }

    this.submissionError = '';
    this.assignFile(documentType, file);
  }

  protected submitApplication(): void {
    if (this.applicationForm.invalid) {
      this.applicationForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.submissionError = '';

    const formValue = this.applicationForm.getRawValue();
    const zipCodes = formValue.zipCodes
      .split(',')
      .map((zip) => zip.trim())
      .filter(Boolean);

    const payload = {
      email: formValue.email,
      fullName: formValue.fullName,
      businessName: formValue.businessName,
      phone: formValue.phone,
      serviceType: formValue.serviceType as 'Locksmith' | 'Glass' | 'Both',
      servicesOffered: formValue.servicesOffered,
      citiesCovered: formValue.citiesCovered,
      state: formValue.state,
      zipCodes,
      yearsOfExperience: Number(formValue.yearsOfExperience),
      emergencyService: formValue.emergencyService,
      workingHours: formValue.workingHours,
      message: formValue.message,
      licenseDocument: this.selectedLicenseFile,
      insuranceDocument: this.selectedInsuranceFile,
      w9Document: this.selectedW9File
    };

    this.applicationService.submitApplication(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.snackBar.open('Application submitted. We will review it and contact you by email.', 'Close', {
          duration: 3200
        });

        this.router.navigate(['/provider/pending']);
      },
      error: (error) => {
        this.isSubmitting = false;

        const message =
          error?.error?.message ||
          error?.error?.title ||
          'Unable to submit application right now. Please retry.';

        this.submissionError = message;
        this.snackBar.open(message, 'Close', {
          duration: 4200
        });
      }
    });
  }

  protected hasControlError(controlName: keyof typeof this.applicationForm.controls, errorName: string): boolean {
    const control = this.applicationForm.controls[controlName];
    return control.touched && control.hasError(errorName);
  }

  protected isCitySelectionDisabled(): boolean {
    return !this.applicationForm.controls.state.value;
  }

  private assignFile(documentType: 'license' | 'insurance' | 'w9', file: File | null): void {
    if (documentType === 'license') {
      this.selectedLicenseFile = file;
      this.selectedLicenseFileName = file?.name ?? '';
      return;
    }

    if (documentType === 'insurance') {
      this.selectedInsuranceFile = file;
      this.selectedInsuranceFileName = file?.name ?? '';
      return;
    }

    this.selectedW9File = file;
    this.selectedW9FileName = file?.name ?? '';
  }
}
