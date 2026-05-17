import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-provider-landing-page',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule],
  templateUrl: './provider-landing-page.component.html',
  styleUrl: './provider-landing-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProviderLandingPageComponent {}
