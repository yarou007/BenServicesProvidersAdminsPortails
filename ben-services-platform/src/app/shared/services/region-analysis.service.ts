import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { CoverageRow, MarketStatus } from '../models/region.model';
import { Provider } from '../models/provider.model';
import { ProviderService } from './provider.service';

export interface RegionSummary {
  ready: number;
  almostReady: number;
  needsMore: number;
  notReady: number;
}

@Injectable({
  providedIn: 'root'
})
export class RegionAnalysisService {
  private readonly providerService = inject(ProviderService);

  readonly coverageRows$ = this.providerService.providers$.pipe(map((providers) => this.buildCoverageRows(providers)));

  readonly summary$ = this.coverageRows$.pipe(
    map((rows) => ({
      ready: rows.filter((row) => row.status === 'Ready for Marketing').length,
      almostReady: rows.filter((row) => row.status === 'Almost Ready').length,
      needsMore: rows.filter((row) => row.status === 'Needs More Providers').length,
      notReady: rows.filter((row) => row.status === 'Not Ready').length
    }))
  );

  getCoverageRowsSnapshot(): CoverageRow[] {
    return this.buildCoverageRows(this.providerService.getProvidersSnapshot());
  }

  getCityCounts$(): Observable<Array<{ city: string; count: number }>> {
    return this.coverageRows$.pipe(
      map((rows) => rows.map((row) => ({ city: row.city, count: row.totalProviders })).sort((a, b) => b.count - a.count))
    );
  }

  getReadinessScores$(): Observable<Array<{ city: string; score: number }>> {
    return this.coverageRows$.pipe(
      map((rows) => rows.map((row) => ({ city: row.city, score: row.readinessScore })).sort((a, b) => b.score - a.score))
    );
  }

  private buildCoverageRows(providers: Provider[]): CoverageRow[] {
    const cityMap = new Map<string, Provider[]>();

    providers.forEach((provider) => {
      const key = `${provider.city}-${provider.state}`;
      const group = cityMap.get(key) ?? [];
      group.push(provider);
      cityMap.set(key, group);
    });

    return Array.from(cityMap.values())
      .map((cityProviders) => this.mapCoverageRow(cityProviders))
      .sort((a, b) => b.readinessScore - a.readinessScore);
  }

  private mapCoverageRow(cityProviders: Provider[]): CoverageRow {
    const firstProvider = cityProviders[0];
    const locksmithCount = cityProviders.filter(
      (provider) => provider.serviceType === 'Locksmith' || provider.serviceType === 'Both'
    ).length;
    const glassCount = cityProviders.filter((provider) => provider.serviceType === 'Glass' || provider.serviceType === 'Both').length;
    const activeProviders = cityProviders.filter((provider) => provider.isActive).length;
    const verifiedProviders = cityProviders.filter(
      (provider) => provider.verificationStatus === 'Verified' || provider.verificationStatus === 'Active'
    ).length;
    const emergencyProviders = cityProviders.filter((provider) => provider.emergencyService).length;

    const serviceDiversity = locksmithCount > 0 && glassCount > 0 ? 5 : locksmithCount > 0 || glassCount > 0 ? 2 : 0;
    const readinessScore = activeProviders + verifiedProviders + emergencyProviders + serviceDiversity;
    const status = this.getStatusByScore(readinessScore);

    return {
      city: firstProvider.city,
      state: firstProvider.state,
      region: firstProvider.region,
      locksmithCount,
      glassCount,
      totalProviders: cityProviders.length,
      verifiedProviders,
      emergencyProviders,
      serviceDiversity,
      readinessScore,
      status,
      recommendedAction: this.getActionByStatus(status)
    };
  }

  private getStatusByScore(score: number): MarketStatus {
    if (score >= 16) {
      return 'Ready for Marketing';
    }

    if (score >= 10) {
      return 'Almost Ready';
    }

    if (score >= 5) {
      return 'Needs More Providers';
    }

    return 'Not Ready';
  }

  private getActionByStatus(status: MarketStatus): string {
    if (status === 'Ready for Marketing') {
      return 'Launch local landing page + paid ads';
    }

    if (status === 'Almost Ready') {
      return 'Publish SEO page and recruit 1-2 more providers';
    }

    if (status === 'Needs More Providers') {
      return 'Run partner outreach and local referral campaigns';
    }

    return 'Prioritize provider acquisition before marketing spend';
  }
}
