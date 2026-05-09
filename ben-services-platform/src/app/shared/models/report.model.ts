export type ReportType = 'Providers' | 'Regions' | 'Applications';

export interface ReportSummaryCard {
  title: string;
  description: string;
  total: number;
  icon: string;
}

export interface CustomReportResult {
  label: string;
  value: number;
  trend: 'up' | 'down' | 'stable';
}
