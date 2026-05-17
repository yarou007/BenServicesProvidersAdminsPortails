import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-public-home-page',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule],
  templateUrl: './public-home-page.component.html',
  styleUrl: './public-home-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PublicHomePageComponent {}
