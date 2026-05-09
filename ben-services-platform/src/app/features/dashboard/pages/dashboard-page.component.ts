import { AsyncPipe, CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { ChartData, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { combineLatest, map } from 'rxjs';
import { ChartCardComponent } from '../../../shared/components/chart-card.component';
import { KpiCardComponent } from '../../../shared/components/kpi-card.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge.component';
import { ApplicationService } from '../../../shared/services/application.service';
import { ProviderService } from '../../../shared/services/provider.service';
import { RegionAnalysisService } from '../../../shared/services/region-analysis.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    BaseChartDirective,
    KpiCardComponent,
    StatusBadgeComponent,
    ChartCardComponent
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardPageComponent {
  private readonly providerService = inject(ProviderService);
  private readonly applicationService = inject(ApplicationService);
  private readonly regionAnalysisService = inject(RegionAnalysisService);

  protected readonly kpiCards$ = combineLatest([
    this.providerService.getKpiSummary(),
    this.applicationService.getPendingCount()
  ]).pipe(
    map(([kpis, pending]) => [
      {
        title: 'Total Providers',
        value: kpis.totalProviders,
        icon: 'groups',
        accent: '#2563eb',
        changeLabel: '+6% this month'
      },
      {
        title: 'Locksmith Providers',
        value: kpis.locksmithProviders,
        icon: 'vpn_key',
        accent: '#0ea5e9',
        changeLabel: '+4 this month'
      },
      {
        title: 'Glass Repair Providers',
        value: kpis.glassProviders,
        icon: 'window',
        accent: '#06b6d4',
        changeLabel: '+3 this month'
      },
      {
        title: 'Verified Providers',
        value: kpis.verifiedProviders,
        icon: 'verified',
        accent: '#16a34a',
        changeLabel: '87% verification rate'
      },
      {
        title: 'Active Providers',
        value: kpis.activeProviders,
        icon: 'toggle_on',
        accent: '#22c55e',
        changeLabel: 'High availability'
      },
      {
        title: 'Pending Applications',
        value: pending,
        icon: 'hourglass_top',
        accent: '#f59e0b',
        changeLabel: 'Needs review today'
      },
      {
        title: 'Cities Ready for Ads',
        value: kpis.citiesReadyForAds,
        icon: 'campaign',
        accent: '#3b82f6',
        changeLabel: 'Launch candidates'
      },
      {
        title: 'Regions with Low Coverage',
        value: kpis.regionsLowCoverage,
        icon: 'location_off',
        accent: '#ef4444',
        changeLabel: 'Recruiting priority',
        isNegative: true
      }
    ])
  );

  protected readonly providersByServiceChart$ = this.providerService.providers$.pipe(
    map((providers) => {
      const locksmith = providers.filter((provider) => provider.serviceType === 'Locksmith').length;
      const glass = providers.filter((provider) => provider.serviceType === 'Glass').length;
      const both = providers.filter((provider) => provider.serviceType === 'Both').length;

      return {
        labels: ['Locksmith', 'Glass', 'Both'],
        datasets: [
          {
            data: [locksmith, glass, both],
            backgroundColor: ['#2563eb', '#10b981', '#f59e0b'],
            borderWidth: 0
          }
        ]
      } satisfies ChartData<'doughnut', number[], string>;
    })
  );

  protected readonly providersByCityChart$ = this.regionAnalysisService.getCityCounts$().pipe(
    map(
      (rows) =>
        ({
          labels: rows.map((row) => row.city),
          datasets: [
            {
              label: 'Providers',
              data: rows.map((row) => row.count),
              backgroundColor: '#1d4ed8',
              borderRadius: 8
            }
          ]
        }) satisfies ChartData<'bar', number[], string>
    )
  );

  protected readonly chartOptions: ChartOptions<'bar' | 'doughnut'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom'
      }
    }
  };

  protected readonly readinessRows$ = this.regionAnalysisService.coverageRows$;

  protected readonly readinessColumns = ['city', 'locksmith', 'glass', 'total', 'status'];

  protected readonly insights$ = this.readinessRows$.pipe(
    map((rows) => {
      const topCity = rows[0];
      const almostReady = rows.find((row) => row.status === 'Almost Ready');
      const needsHelp = rows.find((row) => row.status === 'Needs More Providers' || row.status === 'Not Ready');

      return [
        topCity
          ? `${topCity.city}, ${topCity.state} is ready for SEO + Ads (${topCity.readinessScore}/20 score).`
          : 'No city insights available yet.',
        almostReady
          ? `${almostReady.city}, ${almostReady.state} is nearly launch-ready and only needs a few reinforcements.`
          : 'No almost-ready cities at the moment.',
        needsHelp
          ? `${needsHelp.city}, ${needsHelp.state} needs more providers before ad spend is recommended.`
          : 'Coverage is healthy across all tracked cities.'
      ];
    })
  );
}
