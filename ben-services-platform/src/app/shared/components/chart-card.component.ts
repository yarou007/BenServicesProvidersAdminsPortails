import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-chart-card',
  standalone: true,
  template: `
    <section class="chart-card">
      <header class="chart-head">
        <h3>{{ title() }}</h3>
        <p>{{ subtitle() }}</p>
      </header>
      <div class="chart-body">
        <ng-content />
      </div>
    </section>
  `,
  styleUrl: './chart-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChartCardComponent {
  readonly title = input.required<string>();
  readonly subtitle = input('');
}
