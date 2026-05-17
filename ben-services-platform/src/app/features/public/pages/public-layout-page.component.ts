import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-public-layout-page',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './public-layout-page.component.html',
  styleUrl: './public-layout-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PublicLayoutPageComponent {}
