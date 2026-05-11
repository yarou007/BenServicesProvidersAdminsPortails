import { AsyncPipe, DatePipe, NgIf } from '@angular/common';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, map, switchMap } from 'rxjs';
import { StatusBadgeComponent } from '../../../shared/components/status-badge.component';
import { Provider } from '../../../shared/models/provider.model';
import { ProviderService } from '../../../shared/services/provider.service';

@Component({
  selector: 'app-provider-details-page',
  standalone: true,
  imports: [NgIf, AsyncPipe, DatePipe, RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatSnackBarModule, StatusBadgeComponent],
  templateUrl: './provider-details-page.component.html',
  styleUrl: './provider-details-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProviderDetailsPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly providerService = inject(ProviderService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  protected downloadingW9 = false;
  protected downloadingCoi = false;

  protected readonly provider$ = this.route.paramMap.pipe(
    map((params) => Number(params.get('id'))),
    switchMap((id) => this.providerService.getProviderById(id))
  );

  protected verifyProvider(id: number): void {
    this.providerService.verifyProvider(id).subscribe({
      next: () => this.snackBar.open('Provider has been verified.', 'Close', { duration: 1700 }),
      error: () => this.snackBar.open('Verification failed.', 'Close', { duration: 2100 })
    });
  }

  protected deactivateProvider(id: number): void {
    this.providerService.deactivateProvider(id).subscribe({
      next: () => this.snackBar.open('Provider was deactivated.', 'Close', { duration: 1700 }),
      error: () => this.snackBar.open('Deactivate action failed.', 'Close', { duration: 2100 })
    });
  }

  protected deleteProvider(id: number): void {
    this.providerService.deleteProvider(id).subscribe({
      next: () => {
        this.snackBar.open('Provider was deleted.', 'Close', { duration: 1700 });
        this.router.navigate(['/providers']);
      },
      error: () => this.snackBar.open('Delete failed.', 'Close', { duration: 2100 })
    });
  }

  protected exportProvider(name: string): void {
    this.snackBar.open(`Exported ${name} profile (demo).`, 'Close', { duration: 1700 });
  }

  protected downloadDocument(provider: Provider, type: 'w9' | 'coi'): void {
    if (type === 'w9') {
      if (this.downloadingW9) {
        return;
      }

      this.downloadingW9 = true;
    } else {
      if (this.downloadingCoi) {
        return;
      }

      this.downloadingCoi = true;
    }

    this.providerService
      .downloadProviderDocumentResponse(provider.id, type)
      .pipe(
        finalize(() => {
          if (type === 'w9') {
            this.downloadingW9 = false;
          } else {
            this.downloadingCoi = false;
          }

          this.changeDetectorRef.markForCheck();
        })
      )
      .subscribe({
        next: (response) => this.handleDownloadSuccess(provider, type, response),
        error: (error: unknown) => this.handleDownloadError(error)
      });
  }

  private handleDownloadSuccess(provider: Provider, type: 'w9' | 'coi', response: HttpResponse<Blob>): void {
    const blob = response.body;
    if (!blob) {
      this.snackBar.open('Download failed. Please try again.', 'Close', { duration: 2500 });
      return;
    }

    const backendFileName = this.extractFileNameFromContentDisposition(response.headers.get('content-disposition'));
    const fallbackName = this.buildFallbackDownloadFileName(
      provider,
      type,
      response.headers.get('content-type') ?? blob.type ?? null
    );
    const fileName = backendFileName ?? fallbackName;

    const objectUrl = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.style.display = 'none';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(objectUrl);
  }

  private handleDownloadError(error: unknown): void {
    const status = error instanceof HttpErrorResponse ? error.status : 0;
    const message =
      status === 401
        ? 'Your session expired. Please log in again.'
        : status === 403
          ? 'You do not have permission to download this document.'
          : status === 404
            ? 'This document file is missing or was not found.'
            : 'Download failed. Please try again.';

    this.snackBar.open(message, 'Close', { duration: 3200 });
  }

  private extractFileNameFromContentDisposition(contentDisposition: string | null): string | null {
    if (!contentDisposition) {
      return null;
    }

    const utf8Match = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(contentDisposition);
    if (utf8Match?.[1]) {
      const decoded = this.safeDecodeURIComponent(utf8Match[1]);
      return this.sanitizeFileName(decoded);
    }

    const fallbackMatch = /filename\s*=\s*\"?([^\";]+)\"?/i.exec(contentDisposition);
    if (!fallbackMatch?.[1]) {
      return null;
    }

    return this.sanitizeFileName(fallbackMatch[1]);
  }

  private safeDecodeURIComponent(value: string): string {
    try {
      return decodeURIComponent(value);
    } catch {
      return value;
    }
  }

  private buildFallbackDownloadFileName(provider: Provider, type: 'w9' | 'coi', contentType: string | null): string {
    const prefix = type === 'w9' ? 'W9' : 'COI';
    const preferredName = provider.businessName?.trim() || provider.fullName?.trim() || `provider-${provider.id}`;
    const safeName = this.sanitizeFileName(preferredName).replace(/\.[a-z0-9]{1,10}$/i, '');
    const extension = this.extensionFromContentType(contentType);

    return `${prefix}-${safeName}${extension}`;
  }

  private extensionFromContentType(contentType: string | null): string {
    const normalized = contentType?.split(';')[0].trim().toLowerCase();
    switch (normalized) {
      case 'application/pdf':
        return '.pdf';
      case 'image/jpeg':
      case 'image/jpg':
      case 'image/pjpeg':
        return '.jpg';
      case 'image/png':
        return '.png';
      default:
        return '.pdf';
    }
  }

  private sanitizeFileName(name: string): string {
    const trimmed = (name ?? '').trim().replace(/^['"]+|['"]+$/g, '');
    if (!trimmed) {
      return 'provider-document';
    }

    const extensionMatch = /\.[a-z0-9]{1,10}$/i.exec(trimmed);
    const extension = extensionMatch?.[0].toLowerCase() ?? '';
    const baseName = extension ? trimmed.slice(0, -extension.length) : trimmed;

    const safeBaseName = baseName
      .replace(/[\\/:*?"<>|]/g, ' ')
      .replace(/[^a-zA-Z0-9\s-]/g, ' ')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .replace(/^-+|-+$/g, '');

    return `${safeBaseName || 'provider-document'}${extension}`;
  }
}
