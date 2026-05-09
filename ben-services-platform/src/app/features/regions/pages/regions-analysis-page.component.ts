import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { ChartData, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { map } from 'rxjs';
import { ChartCardComponent } from '../../../shared/components/chart-card.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge.component';
import { RegionAnalysisService } from '../../../shared/services/region-analysis.service';

@Component({
  selector: 'app-regions-analysis-page',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatTableModule, BaseChartDirective, StatusBadgeComponent, ChartCardComponent],
  templateUrl: './regions-analysis-page.component.html',
  styleUrl: './regions-analysis-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegionsAnalysisPageComponent {
  private readonly regionAnalysisService = inject(RegionAnalysisService);

  protected readonly summary$ = this.regionAnalysisService.summary$;
  protected readonly coverageRows$ = this.regionAnalysisService.coverageRows$;

  protected readonly pieChart$ = this.summary$.pipe(
    map(
      (summary) =>
        ({
          labels: ['Ready', 'Almost Ready', 'Needs More', 'Not Ready'],
          datasets: [
            {
              data: [summary.ready, summary.almostReady, summary.needsMore, summary.notReady],
              backgroundColor: ['#22c55e', '#84cc16', '#f59e0b', '#ef4444']
            }
          ]
        }) satisfies ChartData<'pie', number[], string>
    )
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

  protected readonly readinessScoresChart$ = this.regionAnalysisService.getReadinessScores$().pipe(
    map(
      (rows) =>
        ({
          labels: rows.map((row) => row.city),
          datasets: [
            {
              label: 'Readiness Score',
              data: rows.map((row) => row.score),
              backgroundColor: '#16a34a',
              borderRadius: 8
            }
          ]
        }) satisfies ChartData<'bar', number[], string>
    )
  );

  protected readonly pieChartOptions: ChartOptions<'pie'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom'
      }
    }
  };

  protected readonly barChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom'
      }
    },
    scales: {
      y: {
        beginAtZero: true
      }
    }
  };

  protected readonly displayedColumns = [
    'city',
    'state',
    'region',
    'locksmithCount',
    'glassCount',
    'totalProviders',
    'verifiedProviders',
    'emergencyProviders',
    'serviceDiversity',
    'readinessScore',
    'status',
    'recommendedAction'
  ];
}
