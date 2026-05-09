import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [MatCardModule],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Settings</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <p>This section is reserved for future admin and platform configuration settings.</p>
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      mat-card {
        border-radius: 14px;
      }

      p {
        margin: 0;
        color: #64748b;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsPageComponent {}
