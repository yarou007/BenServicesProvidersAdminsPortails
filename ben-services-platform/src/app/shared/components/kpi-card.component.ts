import { MatIconModule } from '@angular/material/icon';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <div class="kpi-card" [style.borderTopColor]="accent()">
      <div class="kpi-head">
        <p class="kpi-title">{{ title() }}</p>
        <mat-icon [style.color]="accent()">{{ icon() }}</mat-icon>
      </div>
      <p class="kpi-value">{{ value() }}</p>
      <p class="kpi-change" [class.negative]="isNegative()">{{ changeLabel() }}</p>
    </div>
  `,
  styleUrl: './kpi-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KpiCardComponent {
  readonly title = input.required<string>();
  readonly value = input.required<string | number>();
  readonly icon = input('insights');
  readonly changeLabel = input('Updated this week');
  readonly accent = input('#2563eb');
  readonly isNegative = input(false);
}
