import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { ClientServiceRequest, CommercialClientRequestSubmitPayload } from '../models/client-request.model';

@Injectable({
  providedIn: 'root'
})
export class ClientRequestService {
  private readonly httpClient = inject(HttpClient);

  submitCommercialRequest(payload: CommercialClientRequestSubmitPayload): Observable<ClientServiceRequest> {
    const formData = new FormData();

    formData.append('companyName', payload.companyName);
    formData.append('contactName', payload.contactName);
    formData.append('phone', payload.phone);
    formData.append('email', payload.email);
    formData.append('serviceCategory', payload.serviceCategory);
    formData.append('urgency', payload.urgency);
    formData.append('address', payload.address);
    formData.append('city', payload.city);
    formData.append('state', payload.state);
    formData.append('zipCode', payload.zipCode);
    formData.append('description', payload.description);

    if (payload.preferredDateTime) {
      formData.append('preferredDateTime', payload.preferredDateTime);
    }

    if (payload.photoFile) {
      formData.append('photoFile', payload.photoFile);
    }

    return this.httpClient.post<ClientServiceRequest>(`${API_BASE_URL}/client-requests/commercial`, formData).pipe(
      catchError((error) => {
        console.error('Failed to submit commercial request', error);
        return throwError(() => error);
      })
    );
  }

  getClientRequests(): Observable<ClientServiceRequest[]> {
    return this.httpClient.get<ClientServiceRequest[]>(`${API_BASE_URL}/client-requests`).pipe(
      catchError((error) => {
        console.error('Failed to load client requests', error);
        return throwError(() => error);
      })
    );
  }
}
