import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-provider-pending-page',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule],
  templateUrl: './provider-pending-page.component.html',
  styleUrl: './provider-pending-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProviderPendingPageComponent {}
