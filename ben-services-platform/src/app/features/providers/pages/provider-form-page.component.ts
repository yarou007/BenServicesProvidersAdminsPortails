import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { of, switchMap, take } from 'rxjs';
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
import { CITY_OPTIONS as LEGACY_CITY_OPTIONS, SERVICE_OPTIONS } from '../../../shared/services/mock-data';
import { Provider } from '../../../shared/models/provider.model';
import { ProviderCreateInput, ProviderService } from '../../../shared/services/provider.service';

const PRIMARY_MARKET_STATES = ['DC', 'VA', 'MD', 'NY'] as const;
type MarketState = (typeof PRIMARY_MARKET_STATES)[number];

const CITIES_BY_STATE: Record<MarketState, string[]> = {
  DC: [
    'Washington DC',
    'Downtown DC',
    'Capitol Hill',
    'Georgetown',
    'K Street',
    'NoMa',
    'Navy Yard',
    'Shaw',
    'Dupont Circle'
  ],
  VA: [
    'Arlington',
    'Alexandria',
    'Fairfax',
    'Ballston',
    'Clarendon',
    'Courthouse',
    'Crystal City',
    'Pentagon City',
    'Rosslyn',
    'Shirlington',
    'Columbia Pike',
    'Cherrydale',
    'Lyon Village',
    'Ashton Heights',
    'Bluemont',
    'Westover',
    'East Falls Church',
    'Glencarlyn',
    'Fairlington',
    'Nauck',
    'Aurora Highlands',
    'Alcova Heights',
    'Arlington Ridge',
    'Penrose',
    'Douglas Park'
  ],
  MD: [
    'Baltimore',
    'Baltimore City',
    'Baltimore County',
    'Towson',
    'Dundalk',
    'Catonsville',
    'Essex',
    'Pikesville',
    'Parkville',
    'Glen Burnie',
    'Downtown Baltimore',
    'Inner Harbor',
    'Canton',
    'Fells Point',
    'Federal Hill',
    'Mount Vernon',
    'Charles Village'
  ],
  NY: ['New York City', 'NYC']
};

const REGION_BY_STATE: Record<MarketState, string> = {
  DC: 'Washington DC / DMV',
  VA: 'Northern Virginia / DMV',
  MD: 'Maryland / Baltimore / DMV',
  NY: 'New York'
};

const MARKET_REGION_OPTIONS = Array.from(new Set(Object.values(REGION_BY_STATE)));
const STATES_MARKER = 'States:';
const CITIES_MARKER = 'Covers:';
const MAX_DOCUMENT_FILE_SIZE_BYTES = 10 * 1024 * 1024;
const ALLOWED_DOCUMENT_EXTENSIONS = new Set(['pdf', 'jpg', 'jpeg', 'png']);
const ALLOWED_DOCUMENT_MIME_TYPES = new Set(['application/pdf', 'image/jpeg', 'image/jpg', 'image/png']);

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
  private readonly destroyRef = inject(DestroyRef);
  private lastSuggestedRegion: string | null = null;

  protected readonly serviceOptions = SERVICE_OPTIONS;
  protected readonly acceptedDocumentFormats = '.pdf,.jpg,.jpeg,.png';
  protected stateOptions: string[] = [...PRIMARY_MARKET_STATES];
  protected regionOptions: string[] = [...MARKET_REGION_OPTIONS];
  protected cityOptions: string[] = [];

  protected editingProviderId: number | null = null;
  protected w9File: File | null = null;
  protected coiFile: File | null = null;
  protected w9FileError = '';
  protected coiFileError = '';
  protected w9ExistingFileUrl: string | null = null;
  protected coiExistingFileUrl: string | null = null;
  protected w9UploadedAt: string | null = null;
  protected coiUploadedAt: string | null = null;

  protected readonly providerForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required]],
    businessName: ['', [Validators.required]],
    phone: ['', [Validators.required, Validators.pattern(/^\(?\d{3}\)?[-\s]?\d{3}[-\s]?\d{4}$/)]],
    email: ['', [Validators.required, Validators.email]],
    serviceType: ['Locksmith' as Provider['serviceType'], [Validators.required]],
    servicesOffered: [[] as string[], [Validators.required]],
    citiesCovered: [[] as string[], [Validators.required]],
    statesCovered: [[] as string[], [Validators.required]],
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
    this.initializeCoverageDependencies();

    const idParam = this.route.snapshot.paramMap.get('id');

    if (!idParam) {
      return;
    }

    const id = Number(idParam);
    this.editingProviderId = id;

    this.providerService.getProviderById(id).pipe(take(1)).subscribe((provider) => {
      if (!provider) {
        return;
      }

      const coveredCities = this.getCoveredCitiesForEdit(provider);
      const coveredStates = this.getCoveredStatesForEdit(provider);
      this.extendLegacyMarketOptions(coveredStates, provider.region);
      this.syncCoverageOptionsForStates(coveredStates, false, coveredCities);
      this.w9ExistingFileUrl = provider.w9FileUrl ?? null;
      this.coiExistingFileUrl = provider.coiFileUrl ?? null;
      this.w9UploadedAt = provider.w9UploadedAt ?? null;
      this.coiUploadedAt = provider.coiUploadedAt ?? null;

      this.providerForm.patchValue(
        {
          fullName: provider.fullName,
          businessName: provider.businessName,
          phone: provider.phone,
          email: provider.email,
          serviceType: provider.serviceType,
          servicesOffered: provider.servicesOffered,
          citiesCovered: coveredCities,
          statesCovered: coveredStates,
          zipCodes: provider.zipCodes.join(', '),
          region: provider.region,
          availability: provider.availability,
          workingHours: provider.workingHours,
          emergencyService: provider.emergencyService,
          verificationStatus: provider.verificationStatus,
          source: provider.source,
          yearsOfExperience: provider.yearsOfExperience,
          notes: provider.notes ?? ''
        },
        { emitEvent: false }
      );
    });
  }

  protected get hasSelectedStates(): boolean {
    return this.providerForm.controls.statesCovered.value.length > 0;
  }

  protected get citiesPlaceholder(): string {
    return this.hasSelectedStates ? 'Select cities or service areas' : 'Select a state first';
  }

  protected get isCreateMode(): boolean {
    return this.editingProviderId === null;
  }

  protected get hasExistingW9Document(): boolean {
    return !!this.w9ExistingFileUrl;
  }

  protected get hasExistingCoiDocument(): boolean {
    return !!this.coiExistingFileUrl;
  }

  protected get w9UploadButtonLabel(): string {
    return this.w9File || this.hasExistingW9Document ? 'Replace file' : 'Upload file';
  }

  protected get coiUploadButtonLabel(): string {
    return this.coiFile || this.hasExistingCoiDocument ? 'Replace file' : 'Upload file';
  }

  protected onW9Selected(event: Event): void {
    this.setDocumentFromInput(event, 'w9');
  }

  protected onCoiSelected(event: Event): void {
    this.setDocumentFromInput(event, 'coi');
  }

  protected removeW9Selection(input: HTMLInputElement): void {
    this.w9File = null;
    this.w9FileError = '';
    input.value = '';
  }

  protected removeCoiSelection(input: HTMLInputElement): void {
    this.coiFile = null;
    this.coiFileError = '';
    input.value = '';
  }

  protected saveProvider(): void {
    if (this.providerForm.invalid) {
      this.providerForm.markAllAsTouched();
      return;
    }

    if (!this.ensureComplianceDocumentsValidForSubmit()) {
      return;
    }

    const providerPayload = this.buildProviderPayload();

    if (this.editingProviderId) {
      this.providerService
        .updateProvider(this.editingProviderId, providerPayload)
        .pipe(
          switchMap((provider) => {
            if (!this.w9File && !this.coiFile) {
              return of(provider);
            }

            return this.providerService.uploadProviderDocuments(this.editingProviderId!, {
              w9File: this.w9File,
              coiFile: this.coiFile
            });
          }),
          take(1)
        )
        .subscribe({
          next: () => {
            this.snackBar.open('Provider updated successfully.', 'Close', { duration: 1900 });
            this.router.navigate(['/providers']);
          },
          error: (error) => {
            this.snackBar.open(this.getApiErrorMessage(error, 'Update failed. Please try again.'), 'Close', { duration: 2600 });
          }
        });
      return;
    }

    const createPayload: ProviderCreateInput = {
      ...providerPayload,
      w9File: this.w9File!,
      coiFile: this.coiFile!
    };

    this.providerService.addProviderWithDocuments(createPayload).pipe(take(1)).subscribe({
      next: () => {
        this.snackBar.open('Provider added successfully.', 'Close', { duration: 1900 });
        this.router.navigate(['/providers']);
      },
      error: (error) => {
        this.snackBar.open(this.getApiErrorMessage(error, 'Creation failed. Please try again.'), 'Close', { duration: 2600 });
      }
    });
  }

  private initializeCoverageDependencies(): void {
    this.syncCoverageOptionsForStates(this.providerForm.controls.statesCovered.value, false);

    this.providerForm.controls.statesCovered.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((states) => {
      this.syncCoverageOptionsForStates(states, true);
    });
  }

  private syncCoverageOptionsForStates(states: string[], resetDependents: boolean, fallbackCities: string[] = []): void {
    this.cityOptions = this.getCityOptionsForStates(states, fallbackCities);

    if (!states.length) {
      this.providerForm.controls.citiesCovered.reset([], { emitEvent: false });
      this.providerForm.controls.citiesCovered.disable({ emitEvent: false });
      this.lastSuggestedRegion = null;
      return;
    }

    this.providerForm.controls.citiesCovered.enable({ emitEvent: false });

    if (resetDependents) {
      this.providerForm.controls.citiesCovered.reset([], { emitEvent: false });
      this.providerForm.controls.zipCodes.reset('', { emitEvent: false });
    }

    this.suggestRegionForStates(states);
  }

  private suggestRegionForStates(states: string[]): void {
    if (states.length !== 1) {
      return;
    }

    const suggestedRegion = REGION_BY_STATE[states[0] as MarketState];

    if (!suggestedRegion) {
      return;
    }

    const regionControl = this.providerForm.controls.region;
    const currentRegion = regionControl.value;
    const canApplySuggestion = !currentRegion || currentRegion === this.lastSuggestedRegion;

    if (canApplySuggestion) {
      regionControl.setValue(suggestedRegion, { emitEvent: false });
      this.lastSuggestedRegion = suggestedRegion;
    }
  }

  private extendLegacyMarketOptions(states: string[], region: string): void {
    const unknownStates = states.filter((state) => !!state && !this.stateOptions.includes(state));
    if (unknownStates.length) {
      this.stateOptions = [...this.stateOptions, ...unknownStates];
    }

    if (region && !this.regionOptions.includes(region)) {
      this.regionOptions = [...this.regionOptions, region];
    }
  }

  private buildCoverageAdminComments(statesCovered: string[], citiesCovered: string[]): string {
    const comments: string[] = [];

    if (statesCovered.length > 1) {
      comments.push(`${STATES_MARKER} ${statesCovered.join(', ')}`);
    }

    if (citiesCovered.length > 1) {
      comments.push(`${CITIES_MARKER} ${citiesCovered.join(', ')}`);
    }

    return comments.join(' | ');
  }

  private getCoveredStatesForEdit(provider: Provider): string[] {
    const coveredFromComments = this.extractListFromComments(provider.adminComments ?? '', STATES_MARKER);
    const covered = [provider.state, ...coveredFromComments].map((state) => state.trim()).filter(Boolean);
    return Array.from(new Set(covered));
  }

  private getCoveredCitiesForEdit(provider: Provider): string[] {
    const coveredFromComments = this.extractListFromComments(provider.adminComments ?? '', CITIES_MARKER);
    const covered = [provider.city, ...coveredFromComments].map((city) => city.trim()).filter(Boolean);
    return Array.from(new Set(covered));
  }

  private extractListFromComments(adminComments: string, marker: string): string[] {
    const segments = adminComments
      .split('|')
      .map((part) => part.trim())
      .filter(Boolean);
    const matchingSegment = segments.find((segment) => segment.startsWith(marker));

    if (!matchingSegment) {
      return adminComments.startsWith(marker)
        ? adminComments
            .slice(marker.length)
            .split(',')
            .map((value) => value.trim())
            .filter(Boolean)
        : [];
    }

    return matchingSegment
      .slice(marker.length)
      .split(',')
      .map((value) => value.trim())
      .filter(Boolean);
  }

  private getCityOptionsForStates(states: string[], fallbackCities: string[] = []): string[] {
    if (!states.length) {
      return [];
    }

    const mappedCities = states.flatMap((state) => CITIES_BY_STATE[state as MarketState] ?? []);
    if (mappedCities.length) {
      const uniqueMappedCities = Array.from(new Set(mappedCities));
      const legacyCitiesForSelection = fallbackCities.filter((city) => !uniqueMappedCities.includes(city));
      return legacyCitiesForSelection.length ? [...uniqueMappedCities, ...legacyCitiesForSelection] : uniqueMappedCities;
    }

    if (fallbackCities.length) {
      return fallbackCities;
    }

    const hasLegacyState = states.some(
      (state) => this.stateOptions.includes(state) && !PRIMARY_MARKET_STATES.includes(state as MarketState)
    );
    if (hasLegacyState) {
      return LEGACY_CITY_OPTIONS;
    }

    return [];
  }

  private setDocumentFromInput(event: Event, documentType: 'w9' | 'coi'): void {
    const input = event.target as HTMLInputElement;
    const selectedFile = input.files?.item(0);

    if (!selectedFile) {
      return;
    }

    const validationError = this.validateDocumentFile(selectedFile);
    if (validationError) {
      if (documentType === 'w9') {
        this.w9File = null;
        this.w9FileError = validationError;
      } else {
        this.coiFile = null;
        this.coiFileError = validationError;
      }
      input.value = '';
      return;
    }

    if (documentType === 'w9') {
      this.w9File = selectedFile;
      this.w9FileError = '';
    } else {
      this.coiFile = selectedFile;
      this.coiFileError = '';
    }
  }

  private ensureComplianceDocumentsValidForSubmit(): boolean {
    if (this.w9FileError || this.coiFileError) {
      return false;
    }

    if (this.isCreateMode) {
      if (!this.w9File) {
        this.w9FileError = 'W-9 form is required.';
      }

      if (!this.coiFile) {
        this.coiFileError = 'Certificate of Insurance is required.';
      }
    }

    return !this.w9FileError && !this.coiFileError;
  }

  private validateDocumentFile(file: File): string | null {
    if (file.size > MAX_DOCUMENT_FILE_SIZE_BYTES) {
      return 'File size must be less than 10 MB.';
    }

    const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (!extension || !ALLOWED_DOCUMENT_EXTENSIONS.has(extension)) {
      return 'Only PDF, JPG, and PNG files are allowed.';
    }

    const normalizedMimeType = file.type.trim().toLowerCase();
    if (normalizedMimeType && !ALLOWED_DOCUMENT_MIME_TYPES.has(normalizedMimeType)) {
      return 'Only PDF, JPG, and PNG files are allowed.';
    }

    return null;
  }

  private buildProviderPayload(): Omit<Provider, 'id' | 'createdAt' | 'updatedAt'> {
    const formValue = this.providerForm.getRawValue();
    const primaryState = formValue.statesCovered[0] ?? '';
    const zipCodes = formValue.zipCodes
      .split(',')
      .map((zip) => zip.trim())
      .filter(Boolean);

    const primaryCity = formValue.citiesCovered[0] ?? 'Unknown';

    return {
      fullName: formValue.fullName,
      businessName: formValue.businessName,
      phone: formValue.phone,
      email: formValue.email,
      serviceType: formValue.serviceType,
      servicesOffered: formValue.servicesOffered,
      city: primaryCity,
      state: primaryState,
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
      adminComments: this.buildCoverageAdminComments(formValue.statesCovered, formValue.citiesCovered),
      verifiedAt:
        formValue.verificationStatus === 'Verified' || formValue.verificationStatus === 'Active'
          ? new Date().toISOString()
          : undefined
    };
  }

  private getApiErrorMessage(error: unknown, fallbackMessage: string): string {
    const apiError = error as { error?: { message?: string } };
    return apiError?.error?.message ?? fallbackMessage;
  }
}
