import { ProviderSource, ServiceType } from './provider.model';

export type ApplicationStatus =
  | 'Pending'
  | 'UnderReview'
  | 'MissingInfo'
  | 'Rejected'
  | 'Accepted'
  | 'Verified'
  | 'Converted';

export interface ProviderApplication {
  id: number;
  userId?: number | null;
  fullName: string;
  businessName: string;
  streetAddress?: string;
  phone: string;
  email: string;
  serviceType: ServiceType;
  servicesOffered: string[];
  states?: string[];
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
  adminNotes?: string | null;
  missingInfoReason?: string | null;
  rejectionReason?: string | null;
  verificationNotes?: string | null;
  convertedProviderId?: number | null;
  submittedAt: string;
  updatedAt?: string;
  reviewedAt?: string | null;
  verifiedAt?: string | null;
  rejectedAt?: string | null;
  licenseFileName?: string;
  licenseFileUrl?: string | null;
  insuranceFileUrl?: string | null;
  w9FileUrl?: string | null;
}

export interface ProviderApplicationSubmitPayload {
  email: string;
  fullName: string;
  businessName: string;
  streetAddress: string;
  phone: string;
  serviceType: ServiceType;
  servicesOffered: string[];
  states: string[];
  citiesCovered: string[];
  zipCodes: string[];
  yearsOfExperience: number;
  emergencyService: boolean;
  workingHours: string;
  message: string;
  licenseDocument?: File | null;
  insuranceDocument?: File | null;
  w9Document?: File | null;
}

export interface ProviderApplicationSubmissionResponse {
  applicationId: number;
  status: ApplicationStatus;
  message: string;
}
