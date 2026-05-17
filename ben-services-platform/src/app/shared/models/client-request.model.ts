export type ClientType = 'Commercial' | 'Residential';

export type ClientRequestStatus = 'New' | 'Reviewed' | 'Assigned' | 'In Progress' | 'Completed' | 'Cancelled';

export type ClientRequestServiceCategory = 'Locksmith' | 'Glass' | 'Door' | 'Board-up' | 'Other';

export type ClientRequestUrgency = 'Emergency' | 'Scheduled';

export interface ClientServiceRequest {
  id: number;
  clientType: ClientType;
  companyName: string;
  contactName: string;
  phone: string;
  email: string;
  serviceCategory: ClientRequestServiceCategory;
  urgency: ClientRequestUrgency;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  description: string;
  preferredDateTime?: string | null;
  status: ClientRequestStatus;
  source: string;
  adminNotes?: string | null;
  photoFileUrl?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CommercialClientRequestSubmitPayload {
  companyName: string;
  contactName: string;
  phone: string;
  email: string;
  serviceCategory: ClientRequestServiceCategory;
  urgency: ClientRequestUrgency;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  description: string;
  preferredDateTime?: string | null;
  photoFile?: File | null;
}
