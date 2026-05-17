import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, tap, throwError } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { AuthService } from '../../core/services/auth.service';
import {
  ProviderApplication,
  ProviderApplicationSubmissionResponse,
  ProviderApplicationSubmitPayload
} from '../models/application.model';
import { ProviderService } from './provider.service';

@Injectable({
  providedIn: 'root'
})
export class ApplicationService {
  private readonly httpClient = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly providerService = inject(ProviderService);
  private readonly applicationsSubject = new BehaviorSubject<ProviderApplication[]>([]);

  readonly applications$ = this.applicationsSubject.asObservable();

  constructor() {
    if (this.authService.isAuthenticated() && this.authService.isAdminRole()) {
      this.refreshApplications().subscribe();
    }
  }

  refreshApplications(): Observable<ProviderApplication[]> {
    return this.httpClient.get<ProviderApplication[]>(`${API_BASE_URL}/applications`).pipe(
      tap((applications) => this.applicationsSubject.next(applications)),
      catchError((error) => {
        console.error('Failed to load applications', error);
        this.applicationsSubject.next([]);
        return of([]);
      })
    );
  }

  getApplicationById(id: number): Observable<ProviderApplication | undefined> {
    return this.httpClient.get<ProviderApplication>(`${API_BASE_URL}/applications/${id}`).pipe(
      tap((application) => this.upsertApplicationInCache(application)),
      map((application) => application),
      catchError((error) => {
        if (error?.status !== 404) {
          console.error('Failed to load application', error);
        }

        return of(this.applicationsSubject.value.find((application) => application.id === id));
      })
    );
  }

  getPendingCount(): Observable<number> {
    return this.applications$.pipe(map((applications) => applications.filter((item) => item.status === 'Pending').length));
  }

  getApplicationsSnapshot(): ProviderApplication[] {
    return this.applicationsSubject.value;
  }

  submitApplication(payload: ProviderApplicationSubmitPayload): Observable<ProviderApplicationSubmissionResponse> {
    const formData = new FormData();

    formData.append('email', payload.email);
    formData.append('fullName', payload.fullName);
    formData.append('businessName', payload.businessName);
    formData.append('streetAddress', payload.streetAddress);
    formData.append('phone', payload.phone);
    formData.append('serviceType', payload.serviceType);
    formData.append('servicesOfferedJson', JSON.stringify(payload.servicesOffered));
    formData.append('statesJson', JSON.stringify(payload.states));
    formData.append('citiesCoveredJson', JSON.stringify(payload.citiesCovered));
    formData.append('state', payload.states[0] ?? '');
    formData.append('zipCodesJson', JSON.stringify(payload.zipCodes));
    formData.append('yearsOfExperience', payload.yearsOfExperience.toString());
    formData.append('emergencyService', payload.emergencyService ? 'true' : 'false');
    formData.append('workingHours', payload.workingHours);
    formData.append('message', payload.message);

    if (payload.licenseDocument) {
      formData.append('licenseDocument', payload.licenseDocument);
    }

    if (payload.insuranceDocument) {
      formData.append('insuranceDocument', payload.insuranceDocument);
    }

    if (payload.w9Document) {
      formData.append('w9Document', payload.w9Document);
    }

    return this.httpClient.post<ProviderApplicationSubmissionResponse>(`${API_BASE_URL}/applications/apply`, formData).pipe(
      catchError((error) => {
        console.error('Failed to submit application', error);
        return throwError(() => error);
      })
    );
  }

  markUnderReview(id: number): Observable<ProviderApplication> {
    return this.httpClient.post<ProviderApplication>(`${API_BASE_URL}/applications/${id}/mark-under-review`, {}).pipe(
      tap((application) => this.upsertApplicationInCache(application)),
      catchError((error) => {
        console.error('Failed to mark application under review', error);
        return throwError(() => error);
      })
    );
  }

  requestMissingInfo(id: number, reason: string): Observable<ProviderApplication> {
    return this.httpClient
      .post<ProviderApplication>(`${API_BASE_URL}/applications/${id}/request-missing-info`, { reason })
      .pipe(
        tap((application) => this.upsertApplicationInCache(application)),
        catchError((error) => {
          console.error('Failed to request missing info', error);
          return throwError(() => error);
        })
      );
  }

  acceptApplication(id: number): Observable<ProviderApplication> {
    return this.httpClient.post<ProviderApplication>(`${API_BASE_URL}/applications/${id}/accept`, {}).pipe(
      tap((application) => this.upsertApplicationInCache(application)),
      catchError((error) => {
        console.error('Failed to accept application', error);
        return throwError(() => error);
      })
    );
  }

  verifyApplication(id: number, verificationNotes?: string): Observable<ProviderApplication> {
    return this.httpClient
      .post<ProviderApplication>(`${API_BASE_URL}/applications/${id}/verify`, { verificationNotes: verificationNotes ?? null })
      .pipe(
        tap((application) => {
          this.upsertApplicationInCache(application);
          this.providerService.refreshProviders().subscribe();
        }),
        catchError((error) => {
          console.error('Failed to verify application', error);
          return throwError(() => error);
        })
      );
  }

  rejectApplication(id: number, reason: string): Observable<ProviderApplication> {
    return this.httpClient.post<ProviderApplication>(`${API_BASE_URL}/applications/${id}/reject`, { reason }).pipe(
      tap((application) => this.upsertApplicationInCache(application)),
      catchError((error) => {
        console.error('Failed to reject application', error);
        return throwError(() => error);
      })
    );
  }

  updateApplicationNotes(id: number, adminNotes: string): Observable<ProviderApplication> {
    return this.httpClient.put<ProviderApplication>(`${API_BASE_URL}/applications/${id}/notes`, { adminNotes }).pipe(
      tap((application) => this.upsertApplicationInCache(application)),
      catchError((error) => {
        console.error('Failed to update application notes', error);
        return throwError(() => error);
      })
    );
  }

  convertToProvider(id: number): Observable<ProviderApplication> {
    return this.httpClient.post<ProviderApplication>(`${API_BASE_URL}/applications/${id}/convert-to-provider`, {}).pipe(
      tap((application) => {
        this.upsertApplicationInCache(application);
        this.providerService.refreshProviders().subscribe();
      }),
      catchError((error) => {
        console.error('Failed to convert application to provider', error);
        return throwError(() => error);
      })
    );
  }

  downloadApplicationDocument(id: number, documentType: 'license' | 'insurance' | 'w9'): Observable<Blob> {
    return this.httpClient
      .get(`${API_BASE_URL}/applications/${id}/documents/${documentType}`, { responseType: 'blob' })
      .pipe(
        catchError((error) => {
          console.error('Failed to download application document', error);
          return throwError(() => error);
        })
      );
  }

  private upsertApplicationInCache(application: ProviderApplication): void {
    const filtered = this.applicationsSubject.value.filter((item) => item.id !== application.id);
    this.applicationsSubject.next([application, ...filtered]);
  }
}
