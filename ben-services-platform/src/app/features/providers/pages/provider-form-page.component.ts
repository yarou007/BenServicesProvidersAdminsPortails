import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { take } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SERVICE_OPTIONS, CITY_OPTIONS, REGION_OPTIONS, STATE_OPTIONS } from '../../../shared/services/mock-data';
import { Provider } from '../../../shared/models/provider.model';
import { ProviderService } from '../../../shared/services/provider.service';

@Component({
  selector: 'app-provider-form-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './provider-form-page.component.html',
  styleUrl: './provider-form-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProviderFormPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly providerService = inject(ProviderService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly serviceOptions = SERVICE_OPTIONS;
  protected readonly cityOptions = CITY_OPTIONS;
  protected readonly stateOptions = STATE_OPTIONS;
  protected readonly regionOptions = REGION_OPTIONS;

  protected editingProviderId: number | null = null;

  protected readonly providerForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required]],
    businessName: ['', [Validators.required]],
    phone: ['', [Validators.required, Validators.pattern(/^\(?\d{3}\)?[-\s]?\d{3}[-\s]?\d{4}$/)]],
    email: ['', [Validators.required, Validators.email]],
    serviceType: ['Locksmith' as Provider['serviceType'], [Validators.required]],
    servicesOffered: [[] as string[], [Validators.required]],
    citiesCovered: [[] as string[], [Validators.required]],
    state: ['', [Validators.required]],
    zipCodes: ['', [Validators.required]],
    region: ['', [Validators.required]],
    availability: ['Daily', [Validators.required]],
    workingHours: ['8:00 AM - 6:00 PM', [Validators.required]],
    emergencyService: [false],
    verificationStatus: ['New' as Provider['verificationStatus'], [Validators.required]],
    source: ['Manual' as Provider['source'], [Validators.required]],
    yearsOfExperience: [3, [Validators.required, Validators.min(0)]],
    notes: ['']
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');

    if (!idParam) {
      return;
    }

    const id = Number(idParam);
    this.providerService.getProviderById(id).pipe(take(1)).subscribe((provider) => {
      if (!provider) {
        return;
      }

      this.editingProviderId = id;
      this.providerForm.patchValue({
        fullName: provider.fullName,
        businessName: provider.businessName,
        phone: provider.phone,
        email: provider.email,
        serviceType: provider.serviceType,
        servicesOffered: provider.servicesOffered,
        citiesCovered: [provider.city],
        state: provider.state,
        zipCodes: provider.zipCodes.join(', '),
        region: provider.region,
        availability: provider.availability,
        workingHours: provider.workingHours,
        emergencyService: provider.emergencyService,
        verificationStatus: provider.verificationStatus,
        source: provider.source,
        yearsOfExperience: provider.yearsOfExperience,
        notes: provider.notes ?? ''
      });
    });
  }

  protected saveProvider(): void {
    if (this.providerForm.invalid) {
      this.providerForm.markAllAsTouched();
      return;
    }

    const formValue = this.providerForm.getRawValue();
    const zipCodes = formValue.zipCodes
      .split(',')
      .map((zip) => zip.trim())
      .filter(Boolean);

    const primaryCity = formValue.citiesCovered[0] ?? 'Unknown';

    const providerPayload: Omit<Provider, 'id' | 'createdAt' | 'updatedAt'> = {
      fullName: formValue.fullName,
      businessName: formValue.businessName,
      phone: formValue.phone,
      email: formValue.email,
      serviceType: formValue.serviceType,
      servicesOffered: formValue.servicesOffered,
      city: primaryCity,
      state: formValue.state,
      zipCodes,
      region: formValue.region,
      emergencyService: formValue.emergencyService,
      availability: formValue.availability,
      workingHours: formValue.workingHours,
      verificationStatus: formValue.verificationStatus,
      isActive: formValue.verificationStatus !== 'Inactive',
      source: formValue.source,
      yearsOfExperience: formValue.yearsOfExperience,
      notes: formValue.notes,
      adminComments: formValue.citiesCovered.length > 1 ? `Covers: ${formValue.citiesCovered.join(', ')}` : '',
      verifiedAt:
        formValue.verificationStatus === 'Verified' || formValue.verificationStatus === 'Active'
          ? new Date().toISOString()
          : undefined
    };

    if (this.editingProviderId) {
      this.providerService.updateProvider(this.editingProviderId, providerPayload).subscribe({
        next: () => {
          this.snackBar.open('Provider updated successfully.', 'Close', { duration: 1900 });
          this.router.navigate(['/providers']);
        },
        error: () => {
          this.snackBar.open('Update failed. Please try again.', 'Close', { duration: 2300 });
        }
      });
    } else {
      this.providerService.addProvider(providerPayload).subscribe({
        next: () => {
          this.snackBar.open('Provider added successfully.', 'Close', { duration: 1900 });
          this.router.navigate(['/providers']);
        },
        error: () => {
          this.snackBar.open('Creation failed. Please try again.', 'Close', { duration: 2300 });
        }
      });
    }
  }
}
