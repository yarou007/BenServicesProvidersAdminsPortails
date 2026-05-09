import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="status-badge" [ngClass]="toneClass()">{{ label() }}</span>
  `,
  styleUrl: './status-badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatusBadgeComponent {
  readonly label = input.required<string>();

  readonly toneClass = computed(() => {
    const normalized = this.label().toLowerCase();

    if (normalized.includes('ready for marketing') || normalized.includes('ready') || normalized.includes('verified') || normalized.includes('active') || normalized.includes('approved')) {
      return 'success';
    }

    if (normalized.includes('almost') || normalized.includes('contacted') || normalized.includes('pending')) {
      return 'warning';
    }

    if (normalized.includes('needs') || normalized.includes('more info')) {
      return 'orange';
    }

    if (normalized.includes('not') || normalized.includes('inactive') || normalized.includes('rejected')) {
      return 'danger';
    }

    return 'neutral';
  });
}
