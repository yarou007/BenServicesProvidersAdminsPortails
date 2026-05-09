import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, switchMap, tap, throwError } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { Provider, ProviderFilters, VerificationStatus } from '../models/provider.model';

export interface ProviderKpis {
  totalProviders: number;
  locksmithProviders: number;
  glassProviders: number;
  verifiedProviders: number;
  activeProviders: number;
  citiesReadyForAds: number;
  regionsLowCoverage: number;
}

@Injectable({
  providedIn: 'root'
})
export class ProviderService {
  private readonly httpClient = inject(HttpClient);
  private readonly providersSubject = new BehaviorSubject<Provider[]>([]);

  readonly providers$ = this.providersSubject.asObservable();

  constructor() {
    this.refreshProviders().subscribe();
  }

  refreshProviders(): Observable<Provider[]> {
    return this.httpClient.get<Provider[]>(`${API_BASE_URL}/providers`).pipe(
      tap((providers) => this.providersSubject.next(providers)),
      catchError((error) => {
        console.error('Failed to load providers', error);
        this.providersSubject.next([]);
        return of([]);
      })
    );
  }

  getProviderById(id: number): Observable<Provider | undefined> {
    return this.httpClient.get<Provider>(`${API_BASE_URL}/providers/${id}`).pipe(
      tap((provider) => this.upsertProviderInCache(provider)),
      map((provider) => provider),
      catchError((error) => {
        if (error?.status !== 404) {
          console.error('Failed to load provider', error);
        }

        return of(this.providersSubject.value.find((provider) => provider.id === id));
      })
    );
  }

  getProviderByIdSnapshot(id: number): Provider | undefined {
    return this.providersSubject.value.find((provider) => provider.id === id);
  }

  getProvidersSnapshot(): Provider[] {
    return this.providersSubject.value;
  }

  addProvider(providerInput: Omit<Provider, 'id' | 'createdAt' | 'updatedAt'>): Observable<Provider> {
    return this.httpClient.post<Provider>(`${API_BASE_URL}/providers`, providerInput).pipe(
      tap((provider) => this.upsertProviderInCache(provider)),
      catchError((error) => {
        console.error('Failed to create provider', error);
        return throwError(() => error);
      })
    );
  }

  updateProvider(id: number, updates: Partial<Provider>): Observable<Provider> {
    const existingProvider = this.getProviderByIdSnapshot(id);

    if (existingProvider) {
      const payload = this.toUpsertPayload({
        ...existingProvider,
        ...updates
      });

      return this.sendProviderUpdate(id, payload);
    }

    return this.getProviderById(id).pipe(
      switchMap((loadedProvider) => {
        if (!loadedProvider) {
          return throwError(() => new Error(`Provider ${id} not found`));
        }

        const payload = this.toUpsertPayload({
          ...loadedProvider,
          ...updates
        });

        return this.sendProviderUpdate(id, payload);
      })
    );
  }

  verifyProvider(id: number): Observable<Provider> {
    return this.httpClient.post<Provider>(`${API_BASE_URL}/providers/${id}/verify`, {}).pipe(
      tap((provider) => this.upsertProviderInCache(provider)),
      catchError((error) => {
        console.error('Failed to verify provider', error);
        return throwError(() => error);
      })
    );
  }

  setProviderStatus(id: number, status: VerificationStatus): Observable<Provider> {
    return this.updateProvider(id, {
      verificationStatus: status,
      isActive: status !== 'Inactive'
    });
  }

  deactivateProvider(id: number): Observable<Provider> {
    return this.httpClient.post<Provider>(`${API_BASE_URL}/providers/${id}/deactivate`, {}).pipe(
      tap((provider) => this.upsertProviderInCache(provider)),
      catchError((error) => {
        console.error('Failed to deactivate provider', error);
        return throwError(() => error);
      })
    );
  }

  deleteProvider(id: number): Observable<void> {
    return this.httpClient.delete<void>(`${API_BASE_URL}/providers/${id}`).pipe(
      tap(() => this.removeProviderFromCache(id)),
      catchError((error) => {
        console.error('Failed to delete provider', error);
        return throwError(() => error);
      })
    );
  }

  filterProviders(filters: ProviderFilters): Observable<Provider[]> {
    return this.providers$.pipe(
      map((providers) =>
        providers.filter((provider) => {
          const searchText = (filters.search ?? '').trim().toLowerCase();
          const matchesSearch =
            !searchText ||
            `${provider.fullName} ${provider.businessName} ${provider.city} ${provider.region}`
              .toLowerCase()
              .includes(searchText);

          const matchesServiceType = filters.serviceType === 'All' || provider.serviceType === filters.serviceType;
          const matchesCity = !filters.city || provider.city.toLowerCase().includes(filters.city.toLowerCase());
          const matchesState = !filters.state || provider.state.toLowerCase().includes(filters.state.toLowerCase());
          const matchesRegion = !filters.region || provider.region.toLowerCase().includes(filters.region.toLowerCase());
          const matchesZip = !filters.zip || provider.zipCodes.some((zip) => zip.includes(filters.zip.trim()));

          const isVerified = provider.verificationStatus === 'Verified' || provider.verificationStatus === 'Active';
          const matchesVerified =
            filters.verified === 'All' ||
            (filters.verified === 'Verified' && isVerified) ||
            (filters.verified === 'Unverified' && !isVerified);

          const matchesActive =
            filters.active === 'All' ||
            (filters.active === 'Active' && provider.isActive) ||
            (filters.active === 'Inactive' && !provider.isActive);

          const matchesEmergency =
            filters.emergency === 'All' ||
            (filters.emergency === 'Yes' && provider.emergencyService) ||
            (filters.emergency === 'No' && !provider.emergencyService);

          const matchesSource = filters.source === 'All' || provider.source === filters.source;

          const createdDate = new Date(provider.createdAt);
          const fromDate = filters.dateFrom ? new Date(filters.dateFrom) : undefined;
          const toDate = filters.dateTo ? new Date(filters.dateTo) : undefined;

          const matchesDateFrom = !fromDate || createdDate >= fromDate;
          const matchesDateTo = !toDate || createdDate <= toDate;

          return (
            matchesSearch &&
            matchesServiceType &&
            matchesCity &&
            matchesState &&
            matchesRegion &&
            matchesZip &&
            matchesVerified &&
            matchesActive &&
            matchesEmergency &&
            matchesSource &&
            matchesDateFrom &&
            matchesDateTo
          );
        })
      )
    );
  }

  getKpiSummary(): Observable<ProviderKpis> {
    return this.providers$.pipe(
      map((providers) => {
        const activeProviders = providers.filter((provider) => provider.isActive);
        const cityScores = this.getCityReadinessScores(providers);

        return {
          totalProviders: providers.length,
          locksmithProviders: providers.filter((provider) => provider.serviceType === 'Locksmith' || provider.serviceType === 'Both')
            .length,
          glassProviders: providers.filter((provider) => provider.serviceType === 'Glass' || provider.serviceType === 'Both').length,
          verifiedProviders: providers.filter(
            (provider) => provider.verificationStatus === 'Verified' || provider.verificationStatus === 'Active'
          ).length,
          activeProviders: activeProviders.length,
          citiesReadyForAds: cityScores.filter((score) => score.score >= 16).length,
          regionsLowCoverage: cityScores.filter((score) => score.score <= 9).length
        };
      })
    );
  }

  private sendProviderUpdate(id: number, provider: Omit<Provider, 'id' | 'createdAt' | 'updatedAt'>): Observable<Provider> {
    return this.httpClient.put<Provider>(`${API_BASE_URL}/providers/${id}`, provider).pipe(
      tap((updatedProvider) => this.upsertProviderInCache(updatedProvider)),
      catchError((error) => {
        console.error('Failed to update provider', error);
        return throwError(() => error);
      })
    );
  }

  private toUpsertPayload(provider: Provider): Omit<Provider, 'id' | 'createdAt' | 'updatedAt'> {
    const { id: _id, createdAt: _createdAt, updatedAt: _updatedAt, ...payload } = provider;
    return payload;
  }

  private upsertProviderInCache(provider: Provider): void {
    const filtered = this.providersSubject.value.filter((item) => item.id !== provider.id);
    this.providersSubject.next([provider, ...filtered]);
  }

  private removeProviderFromCache(id: number): void {
    this.providersSubject.next(this.providersSubject.value.filter((provider) => provider.id !== id));
  }

  private getCityReadinessScores(providers: Provider[]): Array<{ city: string; score: number }> {
    const mapByCity = new Map<string, Provider[]>();

    providers.forEach((provider) => {
      const current = mapByCity.get(provider.city) ?? [];
      current.push(provider);
      mapByCity.set(provider.city, current);
    });

    return Array.from(mapByCity.entries()).map(([city, cityProviders]) => {
      const activeCount = cityProviders.filter((provider) => provider.isActive).length;
      const verifiedCount = cityProviders.filter(
        (provider) => provider.verificationStatus === 'Verified' || provider.verificationStatus === 'Active'
      ).length;
      const emergencyCount = cityProviders.filter((provider) => provider.emergencyService).length;
      const hasLocksmith = cityProviders.some(
        (provider) => provider.serviceType === 'Locksmith' || provider.serviceType === 'Both'
      );
      const hasGlass = cityProviders.some((provider) => provider.serviceType === 'Glass' || provider.serviceType === 'Both');
      const serviceDiversity = hasLocksmith && hasGlass ? 5 : hasLocksmith || hasGlass ? 2 : 0;

      return {
        city,
        score: activeCount + verifiedCount + emergencyCount + serviceDiversity
      };
    });
  }
}
