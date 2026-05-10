import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CITY_OPTIONS, SERVICE_OPTIONS, STATE_OPTIONS } from '../../../shared/services/mock-data';
import { ApplicationService } from '../../../shared/services/application.service';

@Component({
  selector: 'app-provider-application-form-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  templateUrl: './provider-application-form-page.component.html',
  styleUrl: './provider-application-form-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProviderApplicationFormPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly applicationService = inject(ApplicationService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly serviceOptions = SERVICE_OPTIONS;
  protected readonly cityOptions = CITY_OPTIONS;
  protected readonly stateOptions = STATE_OPTIONS;

  protected submitted = false;

  protected readonly applicationForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required]],
    businessName: ['', [Validators.required]],
    phone: ['', [Validators.required, Validators.pattern(/^\(?\d{3}\)?[-\s]?\d{3}[-\s]?\d{4}$/)]],
    email: ['', [Validators.required, Validators.email]],
    serviceType: ['Locksmith', [Validators.required]],
    servicesOffered: [[] as string[], [Validators.required]],
    citiesCovered: [[] as string[], [Validators.required]],
    state: ['', [Validators.required]],
    zipCodes: ['', [Validators.required]],
    yearsOfExperience: [1, [Validators.required, Validators.min(0)]],
    emergencyService: [false],
    workingHours: ['', [Validators.required]],
    message: [''],
    licenseFileName: ['']
  });

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    this.applicationForm.patchValue({
      licenseFileName: file?.name ?? ''
    });
  }

  protected submitApplication(): void {
    if (this.applicationForm.invalid) {
      this.applicationForm.markAllAsTouched();
      return;
    }

    const formValue = this.applicationForm.getRawValue();
    const zipCodes = formValue.zipCodes
      .split(',')
      .map((zip) => zip.trim())
      .filter(Boolean);

    const payload = {
      fullName: formValue.fullName,
      businessName: formValue.businessName,
      phone: formValue.phone,
      email: formValue.email,
      serviceType: formValue.serviceType as 'Locksmith' | 'Glass' | 'Both',
      servicesOffered: formValue.servicesOffered,
      citiesCovered: formValue.citiesCovered,
      city: formValue.citiesCovered[0],
      state: formValue.state,
      zipCodes,
      yearsOfExperience: formValue.yearsOfExperience,
      emergencyService: formValue.emergencyService,
      workingHours: formValue.workingHours,
      message: formValue.message,
      licenseFileName: formValue.licenseFileName
    };

    this.applicationService.submitApplication(payload).subscribe({
      next: () => {
        this.submitted = true;
        this.applicationForm.reset({
          fullName: '',
          businessName: '',
          phone: '',
          email: '',
          serviceType: 'Locksmith',
          servicesOffered: [],
          citiesCovered: [],
          state: '',
          zipCodes: '',
          yearsOfExperience: 1,
          emergencyService: false,
          workingHours: '',
          message: '',
          licenseFileName: ''
        });

        this.snackBar.open('Application Submitted! Thank you for applying.', 'Close', {
          duration: 3200
        });
      },
      error: () => {
        this.snackBar.open('Unable to submit application right now. Please retry.', 'Close', {
          duration: 3200
        });
      }
    });
  }
}
