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
const MAX_DOCUMENT_FILE_SIZE_BYTES = 10 * 1024 * 1024;
const MESSAGE_MAX_LENGTH = 2000;

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
  protected readonly messageMaxLength = MESSAGE_MAX_LENGTH;
  protected cityOptions: string[] = [];

  protected readonly applicationForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required]],
    businessName: ['', [Validators.required]],
    streetAddress: ['', [Validators.required]],
    phone: ['', [Validators.required, Validators.pattern(/^\(?\d{3}\)?[-\s]?\d{3}[-\s]?\d{4}$/)]],
    email: ['', [Validators.required, Validators.email]],
    serviceType: ['Locksmith', [Validators.required]],
    servicesOffered: [[] as string[], [Validators.required]],
    states: [[] as ServiceAreaState[], [Validators.required]],
    citiesCovered: [[] as string[], [Validators.required]],
    zipCodes: ['', [Validators.required]],
    yearsOfExperience: [1, [Validators.required, Validators.min(0)]],
    emergencyService: [false],
    workingHours: ['', [Validators.required]],
    message: ['', [Validators.maxLength(MESSAGE_MAX_LENGTH)]]
  });

  protected isSubmitting = false;
  protected submissionError = '';

  protected selectedLicenseFileName = '';
  protected selectedInsuranceFileName = '';
  protected selectedW9FileName = '';
  protected licenseFileError = '';
  protected insuranceFileError = '';
  protected w9FileError = '';

  private selectedLicenseFile: File | null = null;
  private selectedInsuranceFile: File | null = null;
  private selectedW9File: File | null = null;

  constructor() {
    this.applicationForm.controls.states.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((states) => {
      const selectedStates = (states ?? []).filter(Boolean) as ServiceAreaState[];
      this.syncCityOptionsForStates(selectedStates);
    });

    this.applicationForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.submissionError && !this.isSubmitting) {
        this.submissionError = '';
      }
    });
  }

  protected onFileSelected(event: Event, documentType: 'license' | 'insurance' | 'w9'): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (!file) {
      this.assignFile(documentType, null);
      return;
    }

    const validationError = this.validateSelectedDocument(file);
    if (validationError) {
      this.setFileError(documentType, validationError);
      this.assignFile(documentType, null);
      input.value = '';
      return;
    }

    this.setFileError(documentType, '');
    this.submissionError = '';
    this.assignFile(documentType, file);
  }

  protected submitApplication(): void {
    this.submissionError = '';

    if (this.applicationForm.invalid) {
      this.applicationForm.markAllAsTouched();
      this.focusFirstInvalidControl();
      this.submissionError = 'Please review the highlighted fields before submitting your application.';
      this.isSubmitting = false;
      return;
    }

    if (!this.validateRequiredComplianceDocuments()) {
      this.focusFirstMissingDocumentInput();
      this.isSubmitting = false;
      this.submissionError = 'Please upload all required compliance documents before submitting your application.';
      return;
    }

    this.isSubmitting = true;

    const formValue = this.applicationForm.getRawValue();
    const zipCodes = formValue.zipCodes
      .split(',')
      .map((zip) => zip.trim())
      .filter(Boolean);

    const payload = {
      email: formValue.email,
      fullName: formValue.fullName,
      businessName: formValue.businessName,
      streetAddress: formValue.streetAddress,
      phone: formValue.phone,
      serviceType: formValue.serviceType as 'Locksmith' | 'Glass' | 'Both',
      servicesOffered: formValue.servicesOffered,
      states: formValue.states,
      citiesCovered: formValue.citiesCovered,
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

        const message = this.buildSubmissionErrorMessage(error);

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
    return this.applicationForm.controls.states.value.length === 0;
  }

  protected get messageCharacterCount(): number {
    return this.applicationForm.controls.message.value.length;
  }

  private assignFile(documentType: 'license' | 'insurance' | 'w9', file: File | null): void {
    if (documentType === 'license') {
      this.selectedLicenseFile = file;
      this.selectedLicenseFileName = file?.name ?? '';
      if (file) {
        this.licenseFileError = '';
      }
      return;
    }

    if (documentType === 'insurance') {
      this.selectedInsuranceFile = file;
      this.selectedInsuranceFileName = file?.name ?? '';
      if (file) {
        this.insuranceFileError = '';
      }
      return;
    }

    this.selectedW9File = file;
    this.selectedW9FileName = file?.name ?? '';
    if (file) {
      this.w9FileError = '';
    }
  }

  private syncCityOptionsForStates(states: ServiceAreaState[]): void {
    const mergedCities = states.flatMap((state) => SERVICE_AREAS_BY_STATE[state] ?? []);
    this.cityOptions = Array.from(new Set(mergedCities));

    const currentSelectedCities = this.applicationForm.controls.citiesCovered.value;
    const nextSelectedCities = currentSelectedCities.filter((city) => this.cityOptions.includes(city));

    if (nextSelectedCities.length !== currentSelectedCities.length) {
      this.applicationForm.controls.citiesCovered.setValue(nextSelectedCities);
      this.applicationForm.controls.citiesCovered.markAsTouched();
    }
  }

  private validateSelectedDocument(file: File): string | null {
    if (file.size > MAX_DOCUMENT_FILE_SIZE_BYTES) {
      return 'File size must be less than 10 MB.';
    }

    const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (!ALLOWED_FILE_EXTENSIONS.has(extension)) {
      return 'Only PDF, JPG, JPEG, and PNG files are allowed.';
    }

    return null;
  }

  private setFileError(documentType: 'license' | 'insurance' | 'w9', error: string): void {
    if (documentType === 'license') {
      this.licenseFileError = error;
      return;
    }

    if (documentType === 'insurance') {
      this.insuranceFileError = error;
      return;
    }

    this.w9FileError = error;
  }

  private validateRequiredComplianceDocuments(): boolean {
    if (!this.selectedLicenseFile) {
      this.licenseFileError = 'License document is required.';
    }

    if (!this.selectedInsuranceFile) {
      this.insuranceFileError = 'Insurance / COI document is required.';
    }

    if (!this.selectedW9File) {
      this.w9FileError = 'W-9 document is required.';
    }

    return !this.licenseFileError && !this.insuranceFileError && !this.w9FileError;
  }

  private focusFirstInvalidControl(): void {
    queueMicrotask(() => {
      const firstInvalidControl = document.querySelector<HTMLElement>('[formcontrolname].ng-invalid');
      if (!firstInvalidControl) {
        return;
      }

      firstInvalidControl.scrollIntoView({ behavior: 'smooth', block: 'center' });
      firstInvalidControl.focus();
    });
  }

  private focusFirstMissingDocumentInput(): void {
    queueMicrotask(() => {
      const nextTargetId = !this.selectedLicenseFile
        ? 'license-upload'
        : !this.selectedInsuranceFile
          ? 'insurance-upload'
          : !this.selectedW9File
            ? 'w9-upload'
            : null;

      if (!nextTargetId) {
        return;
      }

      const element = document.getElementById(nextTargetId);
      if (!element) {
        return;
      }

      element.scrollIntoView({ behavior: 'smooth', block: 'center' });
      (element as HTMLElement).focus();
    });
  }

  private buildSubmissionErrorMessage(error: unknown): string {
    const rawError = (error as { error?: { message?: string; title?: string; errors?: Record<string, string> } })?.error;
    if (rawError?.errors) {
      const errorMessages = Object.values(rawError.errors).filter((value) => !!value?.trim());
      if (errorMessages.length > 0) {
        return errorMessages.join(' ');
      }
    }

    return rawError?.message || rawError?.title || 'Unable to submit application right now. Please retry.';
  }
}
