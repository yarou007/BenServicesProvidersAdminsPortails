import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { STATE_OPTIONS } from '../../../shared/services/mock-data';
import { ClientRequestService } from '../../../shared/services/client-request.service';
import { ClientRequestServiceCategory, ClientRequestUrgency } from '../../../shared/models/client-request.model';

const ALLOWED_FILE_EXTENSIONS = new Set(['pdf', 'jpg', 'jpeg', 'png']);

@Component({
  selector: 'app-commercial-request-form-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './commercial-request-form-page.component.html',
  styleUrl: './commercial-request-form-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CommercialRequestFormPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly clientRequestService = inject(ClientRequestService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly stateOptions = STATE_OPTIONS;
  protected readonly categoryOptions: ClientRequestServiceCategory[] = ['Locksmith', 'Glass', 'Door', 'Board-up', 'Other'];
  protected readonly urgencyOptions: ClientRequestUrgency[] = ['Emergency', 'Scheduled'];

  protected readonly requestForm = this.formBuilder.nonNullable.group({
    companyName: ['', [Validators.required]],
    contactName: ['', [Validators.required]],
    phone: ['', [Validators.required, Validators.pattern(/^\(?\d{3}\)?[-\s]?\d{3}[-\s]?\d{4}$/)]],
    email: ['', [Validators.required, Validators.email]],
    serviceCategory: ['Locksmith' as ClientRequestServiceCategory, [Validators.required]],
    urgency: ['Emergency' as ClientRequestUrgency, [Validators.required]],
    address: ['', [Validators.required]],
    city: ['', [Validators.required]],
    state: ['', [Validators.required]],
    zipCode: ['', [Validators.required]],
    description: ['', [Validators.required]],
    preferredDateTime: ['']
  });

  protected isSubmitting = false;
  protected submissionSuccess = false;
  protected submissionError = '';
  protected selectedFileName = '';

  private selectedPhotoFile: File | null = null;

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (!file) {
      this.selectedPhotoFile = null;
      this.selectedFileName = '';
      return;
    }

    const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (!ALLOWED_FILE_EXTENSIONS.has(extension)) {
      this.submissionError = 'Only PDF, JPG, JPEG, and PNG files are allowed.';
      this.selectedPhotoFile = null;
      this.selectedFileName = '';
      input.value = '';
      return;
    }

    this.submissionError = '';
    this.selectedPhotoFile = file;
    this.selectedFileName = file.name;
  }

  protected submitRequest(): void {
    if (this.requestForm.invalid) {
      this.requestForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.submissionError = '';
    this.submissionSuccess = false;

    const formValue = this.requestForm.getRawValue();

    this.clientRequestService
      .submitCommercialRequest({
        companyName: formValue.companyName,
        contactName: formValue.contactName,
        phone: formValue.phone,
        email: formValue.email,
        serviceCategory: formValue.serviceCategory,
        urgency: formValue.urgency,
        address: formValue.address,
        city: formValue.city,
        state: formValue.state,
        zipCode: formValue.zipCode,
        description: formValue.description,
        preferredDateTime: formValue.preferredDateTime || null,
        photoFile: this.selectedPhotoFile
      })
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          this.submissionSuccess = true;
          this.selectedPhotoFile = null;
          this.selectedFileName = '';
          this.requestForm.reset({
            companyName: '',
            contactName: '',
            phone: '',
            email: '',
            serviceCategory: 'Locksmith',
            urgency: 'Emergency',
            address: '',
            city: '',
            state: '',
            zipCode: '',
            description: '',
            preferredDateTime: ''
          });

          this.snackBar.open('Commercial request submitted successfully.', 'Close', {
            duration: 3200
          });
        },
        error: (error) => {
          this.isSubmitting = false;

          const message =
            error?.error?.message || error?.error?.title || 'Unable to submit commercial request right now. Please retry.';

          this.submissionError = message;
          this.snackBar.open(message, 'Close', {
            duration: 4200
          });
        }
      });
  }

  protected hasControlError(controlName: keyof typeof this.requestForm.controls, errorName: string): boolean {
    const control = this.requestForm.controls[controlName];
    return control.touched && control.hasError(errorName);
  }
}
