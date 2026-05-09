import { Injectable, inject } from '@angular/core';
import { combineLatest, map } from 'rxjs';
import { ApplicationService } from './application.service';
import { ProviderService } from './provider.service';
import { RegionAnalysisService } from './region-analysis.service';
import { CustomReportResult, ReportSummaryCard, ReportType } from '../models/report.model';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private readonly providerService = inject(ProviderService);
  private readonly applicationService = inject(ApplicationService);
  private readonly regionAnalysisService = inject(RegionAnalysisService);

  readonly providerReportCards$ = this.providerService.providers$.pipe(
    map((providers) => {
      const activeCount = providers.filter((provider) => provider.isActive).length;
      const locksmithCount = providers.filter(
        (provider) => provider.serviceType === 'Locksmith' || provider.serviceType === 'Both'
      ).length;
      const glassCount = providers.filter((provider) => provider.serviceType === 'Glass' || provider.serviceType === 'Both').length;

      return [
        {
          title: 'Provider Summary',
          description: 'All providers currently indexed in the network',
          total: providers.length,
          icon: 'groups'
        },
        {
          title: 'Service Type Distribution',
          description: 'Providers offering locksmith and/or glass services',
          total: locksmithCount + glassCount,
          icon: 'pie_chart'
        },
        {
          title: 'Active Providers',
          description: 'Providers that are active and available for assignments',
          total: activeCount,
          icon: 'verified'
        }
      ] satisfies ReportSummaryCard[];
    })
  );

  readonly regionReportCards$ = combineLatest([this.regionAnalysisService.summary$, this.regionAnalysisService.coverageRows$]).pipe(
    map(([summary, rows]) => {
      const diversityTotal = rows.reduce((total, row) => total + row.serviceDiversity, 0);

      return [
        {
          title: 'Market Readiness',
          description: 'Cities ready or almost ready for launch campaigns',
          total: summary.ready + summary.almostReady,
          icon: 'trending_up'
        },
        {
          title: 'Coverage Gaps',
          description: 'Cities with low readiness where recruiting is needed',
          total: summary.needsMore + summary.notReady,
          icon: 'report_problem'
        },
        {
          title: 'Service Diversity by Area',
          description: 'Combined diversity score across all covered cities',
          total: diversityTotal,
          icon: 'hub'
        }
      ] satisfies ReportSummaryCard[];
    })
  );

  readonly applicationReportCards$ = this.applicationService.applications$.pipe(
    map((applications) => {
      const pending = applications.filter((application) => application.status === 'Pending').length;
      const approved = applications.filter((application) => application.status === 'Approved').length;
      const rejected = applications.filter((application) => application.status === 'Rejected').length;

      return [
        {
          title: 'Pending Applications',
          description: 'Applications awaiting admin review',
          total: pending,
          icon: 'hourglass_top'
        },
        {
          title: 'Approved / Rejected',
          description: 'Decision outcomes from provider submissions',
          total: approved + rejected,
          icon: 'fact_check'
        }
      ] satisfies ReportSummaryCard[];
    })
  );

  generateCustomReport(reportType: ReportType, dateFrom: string, dateTo: string): CustomReportResult[] {
    const from = dateFrom ? new Date(dateFrom) : undefined;
    const to = dateTo ? new Date(dateTo) : undefined;

    if (reportType === 'Providers') {
      const providers = this.filterByDate(this.providerService.getProvidersSnapshot(), from, to, (item) => item.createdAt);

      return [
        { label: 'Total Added', value: providers.length, trend: 'up' },
        {
          label: 'Verified',
          value: providers.filter((provider) => provider.verificationStatus === 'Verified' || provider.verificationStatus === 'Active')
            .length,
          trend: 'up'
        },
        { label: 'Inactive', value: providers.filter((provider) => !provider.isActive).length, trend: 'down' }
      ];
    }

    if (reportType === 'Applications') {
      const applications = this.filterByDate(
        this.applicationService.getApplicationsSnapshot(),
        from,
        to,
        (item) => item.submittedAt
      );

      return [
        { label: 'Submitted', value: applications.length, trend: 'up' },
        { label: 'Pending', value: applications.filter((item) => item.status === 'Pending').length, trend: 'stable' },
        { label: 'Approved', value: applications.filter((item) => item.status === 'Approved').length, trend: 'up' }
      ];
    }

    const rows = this.regionAnalysisService.getCoverageRowsSnapshot();

    return [
      { label: 'Ready Cities', value: rows.filter((row) => row.status === 'Ready for Marketing').length, trend: 'up' },
      { label: 'Average Score', value: Math.round(this.average(rows.map((row) => row.readinessScore))), trend: 'stable' },
      { label: 'Needs Recruiting', value: rows.filter((row) => row.status === 'Needs More Providers').length, trend: 'down' }
    ];
  }

  private filterByDate<T>(items: T[], from: Date | undefined, to: Date | undefined, accessor: (item: T) => string): T[] {
    return items.filter((item) => {
      const date = new Date(accessor(item));
      const matchesFrom = !from || date >= from;
      const matchesTo = !to || date <= to;
      return matchesFrom && matchesTo;
    });
  }

  private average(values: number[]): number {
    if (!values.length) {
      return 0;
    }

    return values.reduce((total, value) => total + value, 0) / values.length;
  }

}
