import { BreakpointObserver } from '@angular/cdk/layout';
import { CommonModule, DatePipe } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  signal,
  ViewChild,
  inject
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { combineLatest, map, startWith, switchMap, take } from 'rxjs';
import { StatusBadgeComponent } from '../../../shared/components/status-badge.component';
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
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    StatusBadgeComponent
  ],
  templateUrl: './providers-list-page.component.html',
  styleUrl: './providers-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProvidersListPageComponent implements AfterViewInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly providerService = inject(ProviderService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild(MatPaginator) paginator?: MatPaginator;
  @ViewChild(MatSort) sort?: MatSort;

  protected readonly stateOptions = STATE_OPTIONS;
  protected readonly cityOptions = CITY_OPTIONS;
  protected readonly regionOptions = REGION_OPTIONS;
  protected readonly sourceOptions: Array<'All' | ProviderSource> = ['All', 'Google', 'Referral', 'Form', 'Manual'];
  protected readonly isCompact = toSignal(
    this.breakpointObserver.observe('(max-width: 1180px)').pipe(map((state) => state.matches)),
    { initialValue: false }
  );
  protected readonly showAdvancedFilters = signal(false);

  protected readonly displayedColumns = [
    'provider',
    'service',
    'coverage',
    'availability',
    'verification',
    'source',
    'dateAdded',
    'actions'
  ];

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

  protected readonly dataSource = new MatTableDataSource<Provider>([]);
  protected filteredProviders: Provider[] = [];
  protected totalProvidersCount = 0;
  protected activeProvidersCount = 0;
  protected verifiedProvidersCount = 0;
  protected readonly defaultPageSize = 10;

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

          return {
            ...formValue,
            search: searchValue
          } as ProviderFilters;
        }),
        switchMap((filters) => this.providerService.filterProviders(filters)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((providers) => {
        this.filteredProviders = providers;
        this.totalProvidersCount = this.providerService.getProvidersSnapshot().length;
        this.activeProvidersCount = providers.filter((provider) => provider.isActive).length;
        this.verifiedProvidersCount = providers.filter(
          (provider) => provider.verificationStatus === 'Verified' || provider.verificationStatus === 'Active'
        ).length;
        this.dataSource.data = providers;

        if (this.paginator) {
          this.paginator.firstPage();
        }
      });
  }

  ngAfterViewInit(): void {
    this.dataSource.sortingDataAccessor = (row, property) => {
      switch (property) {
        case 'provider':
          return `${row.fullName} ${row.businessName}`.toLowerCase();
        case 'service':
          return row.serviceType;
        case 'coverage':
          return `${row.city} ${row.state} ${row.region}`.toLowerCase();
        case 'availability':
          return `${row.availability} ${row.workingHours}`.toLowerCase();
        case 'verification':
          return row.verificationStatus;
        case 'source':
          return row.source;
        case 'dateAdded':
          return row.createdAt;
        default:
          return '';
      }
    };

    this.dataSource.sort = this.sort ?? null;
    this.dataSource.paginator = this.paginator ?? null;
  }

  protected toggleAdvancedFilters(): void {
    this.showAdvancedFilters.update((value) => !value);
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

  protected getServicePreview(provider: Provider): string {
    const visibleServices = provider.servicesOffered.slice(0, 2);
    const hiddenCount = provider.servicesOffered.length - visibleServices.length;
    const base = visibleServices.join(', ');

    return hiddenCount > 0 ? `${base} +${hiddenCount}` : base;
  }

  protected verifyProvider(id: number): void {
    this.providerService.verifyProvider(id).pipe(take(1)).subscribe({
      next: () => {
        this.snackBar.open('Provider verified successfully.', 'Close', { duration: 1800 });
      },
      error: () => {
        this.snackBar.open('Unable to verify provider right now.', 'Close', { duration: 2200 });
      }
    });
  }

  protected deactivateProvider(id: number): void {
    this.providerService.deactivateProvider(id).pipe(take(1)).subscribe({
      next: () => {
        this.snackBar.open('Provider moved to inactive status.', 'Close', { duration: 1800 });
      },
      error: () => {
        this.snackBar.open('Unable to update provider status.', 'Close', { duration: 2200 });
      }
    });
  }

  protected deleteProvider(id: number): void {
    this.providerService.deleteProvider(id).pipe(take(1)).subscribe({
      next: () => {
        this.snackBar.open('Provider removed from directory.', 'Close', { duration: 1800 });
      },
      error: () => {
        this.snackBar.open('Unable to delete provider.', 'Close', { duration: 2200 });
      }
    });
  }
}
