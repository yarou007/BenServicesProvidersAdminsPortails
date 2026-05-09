import { ProviderSource, ServiceType } from './provider.model';

export type ApplicationStatus = 'Pending' | 'Approved' | 'Rejected' | 'More Info Requested';

export interface ProviderApplication {
  id: number;
  fullName: string;
  businessName: string;
  phone: string;
  email: string;
  serviceType: ServiceType;
  servicesOffered: string[];
  citiesCovered: string[];
  city: string;
  state: string;
  zipCodes: string[];
  yearsOfExperience: number;
  emergencyService: boolean;
  workingHours: string;
  message: string;
  source: ProviderSource;
  status: ApplicationStatus;
  submittedAt: string;
  licenseFileName?: string;
}
