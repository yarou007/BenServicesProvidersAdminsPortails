import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, tap, throwError } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { ProviderApplication } from '../models/application.model';
import { ProviderService } from './provider.service';

@Injectable({
  providedIn: 'root'
})
export class ApplicationService {
  private readonly httpClient = inject(HttpClient);
  private readonly providerService = inject(ProviderService);
  private readonly applicationsSubject = new BehaviorSubject<ProviderApplication[]>([]);

  readonly applications$ = this.applicationsSubject.asObservable();

  constructor() {
    this.refreshApplications().subscribe();
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

  submitApplication(applicationInput: Omit<ProviderApplication, 'id' | 'status' | 'submittedAt' | 'source'>): Observable<ProviderApplication> {
    return this.httpClient.post<ProviderApplication>(`${API_BASE_URL}/applications`, applicationInput).pipe(
      tap((application) => this.upsertApplicationInCache(application)),
      catchError((error) => {
        console.error('Failed to submit application', error);
        return throwError(() => error);
      })
    );
  }

  approveApplication(id: number): Observable<ProviderApplication> {
    return this.httpClient.post<ProviderApplication>(`${API_BASE_URL}/applications/${id}/approve`, {}).pipe(
      tap((application) => {
        this.upsertApplicationInCache(application);
        this.providerService.refreshProviders().subscribe();
      }),
      catchError((error) => {
        console.error('Failed to approve application', error);
        return throwError(() => error);
      })
    );
  }

  rejectApplication(id: number): Observable<ProviderApplication> {
    return this.httpClient.post<ProviderApplication>(`${API_BASE_URL}/applications/${id}/reject`, {}).pipe(
      tap((application) => this.upsertApplicationInCache(application)),
      catchError((error) => {
        console.error('Failed to reject application', error);
        return throwError(() => error);
      })
    );
  }

  requestMoreInfo(id: number): Observable<ProviderApplication> {
    return this.httpClient.post<ProviderApplication>(`${API_BASE_URL}/applications/${id}/request-more-info`, {}).pipe(
      tap((application) => this.upsertApplicationInCache(application)),
      catchError((error) => {
        console.error('Failed to request more info', error);
        return throwError(() => error);
      })
    );
  }

  private upsertApplicationInCache(application: ProviderApplication): void {
    const filtered = this.applicationsSubject.value.filter((item) => item.id !== application.id);
    this.applicationsSubject.next([application, ...filtered]);
  }
}
