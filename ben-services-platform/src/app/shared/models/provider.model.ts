export type ServiceType = 'Locksmith' | 'Glass' | 'Both';

export type VerificationStatus = 'New' | 'Contacted' | 'Verified' | 'Active' | 'Inactive';

export type ProviderSource = 'Google' | 'Referral' | 'Form' | 'Manual' | 'PublicWebsite';

export interface Provider {
  id: number;
  fullName: string;
  businessName: string;
  streetAddress?: string;
  phone: string;
  email: string;
  serviceType: ServiceType;
  servicesOffered: string[];
  states?: string[];
  city: string;
  state: string;
  zipCodes: string[];
  region: string;
  emergencyService: boolean;
  availability: string;
  workingHours: string;
  verificationStatus: VerificationStatus;
  isActive: boolean;
  source: ProviderSource;
  yearsOfExperience: number;
  notes?: string;
  adminComments?: string;
  hasW9File?: boolean;
  hasCoiFile?: boolean;
  w9FileUrl?: string;
  coiFileUrl?: string;
  w9UploadedAt?: string;
  coiUploadedAt?: string;
  createdAt: string;
  updatedAt: string;
  verifiedAt?: string;
}

export interface ProviderFilters {
  search: string;
  serviceType: 'All' | ServiceType;
  city: string;
  state: string;
  region: string;
  zip: string;
  verified: 'All' | 'Verified' | 'Unverified';
  active: 'All' | 'Active' | 'Inactive';
  emergency: 'All' | 'Yes' | 'No';
  source: 'All' | ProviderSource;
  dateFrom: string | null;
  dateTo: string | null;
}
