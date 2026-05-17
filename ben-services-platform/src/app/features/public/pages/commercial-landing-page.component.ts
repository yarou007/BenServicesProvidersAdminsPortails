import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-commercial-landing-page',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule],
  templateUrl: './commercial-landing-page.component.html',
  styleUrl: './commercial-landing-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CommercialLandingPageComponent {}
