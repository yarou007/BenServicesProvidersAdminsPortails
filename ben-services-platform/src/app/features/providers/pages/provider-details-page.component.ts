import { AsyncPipe, DatePipe, NgIf } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map, switchMap } from 'rxjs';
import { StatusBadgeComponent } from '../../../shared/components/status-badge.component';
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
}
