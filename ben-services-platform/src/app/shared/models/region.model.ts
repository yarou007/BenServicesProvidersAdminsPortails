export type MarketStatus = 'Ready for Marketing' | 'Almost Ready' | 'Needs More Providers' | 'Not Ready';

export interface CoverageRow {
  city: string;
  state: string;
  region: string;
  locksmithCount: number;
  glassCount: number;
  totalProviders: number;
  verifiedProviders: number;
  emergencyProviders: number;
  serviceDiversity: number;
  readinessScore: number;
  status: MarketStatus;
  recommendedAction: string;
}
