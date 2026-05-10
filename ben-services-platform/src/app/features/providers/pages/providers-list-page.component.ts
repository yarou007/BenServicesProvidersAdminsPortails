import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { combineLatest, map, startWith, switchMap, take } from 'rxjs';
import { Provider, ProviderFilters, ProviderSource } from '../../../shared/models/provider.model';
import { CITY_OPTIONS, REGION_OPTIONS, STATE_OPTIONS } from '../../../shared/services/mock-data';
import { ProviderService } from '../../../shared/services/provider.service';

@Component({
  selector: 'app-providers-list-page',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatMenuModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './providers-list-page.component.html',
  styleUrl: './providers-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProvidersListPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly providerService = inject(ProviderService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private previousFiltersSignature = '';

  protected readonly stateOptions = STATE_OPTIONS;
  protected readonly cityOptions = CITY_OPTIONS;
  protected readonly regionOptions = REGION_OPTIONS;
  protected readonly sourceOptions: Array<'All' | ProviderSource> = ['All', 'Google', 'Referral', 'Form', 'Manual'];
  protected readonly showAdvancedFilters = signal(false);
  protected readonly isLoading = signal(true);

  protected readonly filtersForm = this.formBuilder.group({
    search: '',
    serviceType: 'All' as ProviderFilters['serviceType'],
    city: '',
    state: '',
    region: '',
    zip: '',
    verified: 'All' as ProviderFilters['verified'],
    active: 'All' as ProviderFilters['active'],
    emergency: 'All' as ProviderFilters['emergency'],
    source: 'All' as ProviderFilters['source'],
    dateFrom: null as string | null,
    dateTo: null as string | null
  });

  protected filteredProviders: Provider[] = [];
  protected paginatedProviders: Provider[] = [];
  protected totalProvidersCount = 0;
  protected activeProvidersCount = 0;
  protected verifiedProvidersCount = 0;
  protected pendingProvidersCount = 0;
  protected currentPage = 1;
  protected readonly pageSize = 5;
  protected totalPages = 1;
  protected paginationStart = 0;
  protected paginationEnd = 0;
  protected visiblePageNumbers: number[] = [];

  constructor() {
    combineLatest([
      this.route.queryParamMap.pipe(
        map((params) => params.get('q') ?? ''),
        startWith('')
      ),
      this.filtersForm.valueChanges.pipe(startWith(this.filtersForm.getRawValue()))
    ])
      .pipe(
        map(([query, formValue]) => {
          const searchValue = query || formValue.search || '';

          if (query && query !== formValue.search) {
            this.filtersForm.patchValue({ search: query }, { emitEvent: false });
          }

          const filters = {
            ...formValue,
            search: searchValue
          } as ProviderFilters;

          return {
            filters,
            filtersSignature: this.getFiltersSignature(filters)
          };
        }),
        switchMap(({ filters, filtersSignature }) =>
          this.providerService.filterProviders(filters).pipe(map((providers) => ({ providers, filtersSignature })))
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(({ providers, filtersSignature }) => {
        const allProviders = this.providerService.getProvidersSnapshot();
        const sortedProviders = this.getSortedProviders(providers);
        const shouldResetPage = filtersSignature !== this.previousFiltersSignature;

        this.previousFiltersSignature = filtersSignature;
        this.filteredProviders = sortedProviders;
        this.totalProvidersCount = allProviders.length;
        this.activeProvidersCount = allProviders.filter((provider) => provider.isActive).length;
        this.verifiedProvidersCount = allProviders.filter((provider) => this.isVerified(provider)).length;
        this.pendingProvidersCount = allProviders.filter((provider) => {
          const leadStatus = this.getLeadStatus(provider);
          return leadStatus === 'New' || leadStatus === 'Contacted';
        }).length;

        if (shouldResetPage) {
          this.currentPage = 1;
        }

        this.updatePagination();
      });

    this.providerService.refreshProviders().pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.isLoading.set(false),
      error: () => this.isLoading.set(false)
    });
  }

  protected toggleAdvancedFilters(): void {
    this.showAdvancedFilters.update((value) => !value);
  }

  protected updatePagination(): void {
    const totalResults = this.filteredProviders.length;

    this.totalPages = Math.max(1, Math.ceil(totalResults / this.pageSize));
    this.currentPage = Math.min(Math.max(this.currentPage, 1), this.totalPages);

    if (!totalResults) {
      this.paginationStart = 0;
      this.paginationEnd = 0;
      this.paginatedProviders = [];
      this.visiblePageNumbers = [];
      return;
    }

    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;

    this.paginatedProviders = this.filteredProviders.slice(startIndex, endIndex);
    this.paginationStart = startIndex + 1;
    this.paginationEnd = Math.min(endIndex, totalResults);
    this.visiblePageNumbers = this.buildVisiblePageNumbers();
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }

    this.currentPage = page;
    this.updatePagination();
  }

  protected nextPage(): void {
    this.goToPage(this.currentPage + 1);
  }

  protected previousPage(): void {
    this.goToPage(this.currentPage - 1);
  }

  protected firstPage(): void {
    this.goToPage(1);
  }

  protected lastPage(): void {
    this.goToPage(this.totalPages);
  }

  protected get hasMultiplePages(): boolean {
    return this.totalPages > 1;
  }

  protected get canGoPrevious(): boolean {
    return this.currentPage > 1;
  }

  protected get canGoNext(): boolean {
    return this.currentPage < this.totalPages;
  }

  protected get showLeadingFirstPage(): boolean {
    return this.visiblePageNumbers.length > 0 && this.visiblePageNumbers[0] > 1;
  }

  protected get showTrailingLastPage(): boolean {
    return (
      this.visiblePageNumbers.length > 0 &&
      this.visiblePageNumbers[this.visiblePageNumbers.length - 1] < this.totalPages
    );
  }

  protected get showLeadingEllipsis(): boolean {
    return this.visiblePageNumbers.length > 0 && this.visiblePageNumbers[0] > 2;
  }

  protected get showTrailingEllipsis(): boolean {
    return (
      this.visiblePageNumbers.length > 0 &&
      this.visiblePageNumbers[this.visiblePageNumbers.length - 1] < this.totalPages - 1
    );
  }

  protected get hasActiveFilters(): boolean {
    const formValue = this.filtersForm.getRawValue();

    return (
      !!formValue.search ||
      formValue.serviceType !== 'All' ||
      !!formValue.city ||
      !!formValue.state ||
      !!formValue.region ||
      !!formValue.zip ||
      formValue.verified !== 'All' ||
      formValue.active !== 'All' ||
      formValue.emergency !== 'All' ||
      formValue.source !== 'All' ||
      !!formValue.dateFrom ||
      !!formValue.dateTo
    );
  }

  protected resetFilters(): void {
    this.filtersForm.reset({
      search: '',
      serviceType: 'All',
      city: '',
      state: '',
      region: '',
      zip: '',
      verified: 'All',
      active: 'All',
      emergency: 'All',
      source: 'All',
      dateFrom: null,
      dateTo: null
    });
  }

  protected getLeadStatus(provider: Provider): 'New' | 'Contacted' | 'Qualified' | 'Rejected' {
    switch (provider.verificationStatus) {
      case 'New':
        return 'New';
      case 'Contacted':
        return 'Contacted';
      case 'Inactive':
        return 'Rejected';
      default:
        return 'Qualified';
    }
  }

  protected isVerified(provider: Provider): boolean {
    return provider.verificationStatus === 'Verified' || provider.verificationStatus === 'Active';
  }

  protected getVisibleServiceTags(provider: Provider): string[] {
    return provider.servicesOffered.slice(0, 2);
  }

  protected getRemainingServiceCount(provider: Provider): number {
    return Math.max(provider.servicesOffered.length - 2, 0);
  }

  protected getZipPreview(provider: Provider): string {
    const visible = provider.zipCodes.slice(0, 3).join(', ');
    const hidden = Math.max(provider.zipCodes.length - 3, 0);
    return hidden > 0 ? `${visible} +${hidden}` : visible;
  }

  protected getSourceLabel(source: ProviderSource): string {
    switch (source) {
      case 'Google':
      case 'Form':
        return 'Website';
      case 'Manual':
        return 'Manual';
      case 'Referral':
        return 'Import';
      default:
        return source;
    }
  }

  protected getSourceTone(source: ProviderSource): 'manual' | 'website' | 'referral' {
    switch (source) {
      case 'Manual':
        return 'manual';
      case 'Referral':
        return 'referral';
      default:
        return 'website';
    }
  }

  protected getVerificationLabel(provider: Provider): 'Verified' | 'Not verified' {
    return this.isVerified(provider) ? 'Verified' : 'Not verified';
  }

  protected getActiveLabel(provider: Provider): 'Active' | 'Inactive' {
    return provider.isActive ? 'Active' : 'Inactive';
  }

  protected toggleVerification(provider: Provider): void {
    if (this.isVerified(provider)) {
      this.unverifyProvider(provider);
      return;
    }

    this.verifyProvider(provider.id);
  }

  protected toggleActivation(provider: Provider): void {
    if (provider.isActive) {
      this.deactivateProvider(provider.id);
      return;
    }

    this.activateProvider(provider.id);
  }

  protected getVerificationActionLabel(provider: Provider): 'Verify' | 'Unverify' {
    return this.isVerified(provider) ? 'Unverify' : 'Verify';
  }

  protected getActivationActionLabel(provider: Provider): 'Activate' | 'Deactivate' {
    return provider.isActive ? 'Deactivate' : 'Activate';
  }

  protected exportProviders(): void {
    if (!this.filteredProviders.length) {
      this.snackBar.open('No providers to export.', 'Close', { duration: 1800 });
      return;
    }

    const headers = [
      'Provider',
      'Business',
      'Phone',
      'Email',
      'ServiceType',
      'Services',
      'City',
      'State',
      'Region',
      'ZIP',
      'Availability',
      'WorkingHours',
      'LeadStatus',
      'Verification',
      'ActiveStatus',
      'Source',
      'DateAdded'
    ];

    const rows = this.filteredProviders.map((provider) => [
      provider.fullName,
      provider.businessName,
      provider.phone,
      provider.email,
      provider.serviceType,
      provider.servicesOffered.join(' | '),
      provider.city,
      provider.state,
      provider.region,
      provider.zipCodes.join(' | '),
      provider.availability,
      provider.workingHours,
      this.getLeadStatus(provider),
      this.getVerificationLabel(provider),
      this.getActiveLabel(provider),
      this.getSourceLabel(provider.source),
      new Date(provider.createdAt).toISOString()
    ]);

    const csv = [headers, ...rows]
      .map((line) => line.map((value) => this.escapeCsv(String(value ?? ''))).join(','))
      .join('\n');

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const fileName = `providers-${new Date().toISOString().slice(0, 10)}.csv`;
    const link = document.createElement('a');

    link.href = URL.createObjectURL(blob);
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(link.href);

    this.snackBar.open('Providers exported.', 'Close', { duration: 1600 });
  }

  protected confirmDeleteProvider(provider: Provider): void {
    const shouldDelete = window.confirm(`Delete ${provider.fullName} from providers?`);

    if (!shouldDelete) {
      return;
    }

    this.deleteProvider(provider.id);
  }

  private unverifyProvider(provider: Provider): void {
    this.providerService
      .updateProvider(provider.id, {
        verificationStatus: 'Contacted'
      })
      .pipe(take(1))
      .subscribe({
        next: () => {
          this.snackBar.open('Provider marked as not verified.', 'Close', { duration: 1800 });
        },
        error: () => {
          this.snackBar.open('Unable to update verification right now.', 'Close', { duration: 2200 });
        }
      });
  }

  private activateProvider(id: number): void {
    this.providerService.setProviderStatus(id, 'Active').pipe(take(1)).subscribe({
      next: () => {
        this.snackBar.open('Provider activated.', 'Close', { duration: 1800 });
      },
      error: () => {
        this.snackBar.open('Unable to activate provider right now.', 'Close', { duration: 2200 });
      }
    });
  }

  private verifyProvider(id: number): void {
    this.providerService.verifyProvider(id).pipe(take(1)).subscribe({
      next: () => {
        this.snackBar.open('Provider verified successfully.', 'Close', { duration: 1800 });
      },
      error: () => {
        this.snackBar.open('Unable to verify provider right now.', 'Close', { duration: 2200 });
      }
    });
  }

  private deactivateProvider(id: number): void {
    this.providerService.deactivateProvider(id).pipe(take(1)).subscribe({
      next: () => {
        this.snackBar.open('Provider moved to inactive status.', 'Close', { duration: 1800 });
      },
      error: () => {
        this.snackBar.open('Unable to update provider status.', 'Close', { duration: 2200 });
      }
    });
  }

  private deleteProvider(id: number): void {
    this.providerService.deleteProvider(id).pipe(take(1)).subscribe({
      next: () => {
        this.snackBar.open('Provider removed from directory.', 'Close', { duration: 1800 });
      },
      error: () => {
        this.snackBar.open('Unable to delete provider.', 'Close', { duration: 2200 });
      }
    });
  }

  private escapeCsv(value: string): string {
    return `"${value.replace(/"/g, '""')}"`;
  }

  private buildVisiblePageNumbers(): number[] {
    if (this.totalPages <= 1) {
      return [];
    }

    if (this.totalPages <= 7) {
      return Array.from({ length: this.totalPages }, (_, index) => index + 1);
    }

    const maxVisibleAroundCurrent = 5;
    let start = Math.max(1, this.currentPage - 2);
    let end = Math.min(this.totalPages, start + maxVisibleAroundCurrent - 1);

    if (end - start + 1 < maxVisibleAroundCurrent) {
      start = Math.max(1, end - maxVisibleAroundCurrent + 1);
    }

    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  }

  private getFiltersSignature(filters: ProviderFilters): string {
    return JSON.stringify(filters);
  }

  private getSortedProviders(providers: Provider[]): Provider[] {
    return [...providers];
  }
}
