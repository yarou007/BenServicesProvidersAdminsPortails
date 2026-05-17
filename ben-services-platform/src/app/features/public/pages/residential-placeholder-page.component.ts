import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-residential-placeholder-page',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule],
  templateUrl: './residential-placeholder-page.component.html',
  styleUrl: './residential-placeholder-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResidentialPlaceholderPageComponent {}
