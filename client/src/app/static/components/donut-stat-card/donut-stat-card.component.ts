import { Component, computed, input } from '@angular/core';
import { NgApexchartsModule } from 'ng-apexcharts';
import { ColorSwatchComponent } from '../color-swatch/color-swatch.component';

export interface DonutStatItem {
  label: string;
  value: number;
  color: string;
}

@Component({
  selector: 'app-donut-stat-card',
  imports: [NgApexchartsModule, ColorSwatchComponent],
  template: `
    <div
      class="border-border bg-card flex h-full min-h-24 flex-col rounded border p-6 shadow-sm">
      <div class="mb-2 flex items-center justify-between">
        <h3 class="text-foreground text-base font-semibold">{{ title() }}</h3>
        <ng-content select="[card-actions]" />
      </div>

      @if (hasData()) {
        <div class="flex items-center gap-6">
          <div class="shrink-0">
            <apx-chart
              [series]="series()"
              [labels]="labels()"
              [colors]="colors()"
              [chart]="chart"
              [plotOptions]="plotOptions()"
              [stroke]="stroke"
              [legend]="legend"
              [dataLabels]="dataLabels"
              [tooltip]="tooltip" />
          </div>

          <ul class="flex flex-1 flex-col gap-3">
            @for (item of items(); track item.label) {
              <li class="flex items-center gap-3">
                <app-color-swatch [color]="item.color" />
                <span class="text-muted flex-1 truncate text-sm">
                  {{ item.label }}
                </span>
                <span class="text-foreground text-sm font-semibold">
                  {{ item.value.toLocaleString() }}
                </span>
              </li>
            }
          </ul>
        </div>
      } @else {
        <div
          class="text-muted flex min-h-40 items-center justify-center text-sm">
          {{ emptyMessage() }}
        </div>
      }
    </div>
  `,
})
export class DonutStatCardComponent {
  readonly title = input.required<string>();
  readonly items = input<DonutStatItem[]>([]);
  /** Centre figure. Defaults to the sum of all item values when not provided. */
  readonly total = input<number | null>(null);
  readonly totalLabel = input('Total');
  readonly emptyMessage = input('No data to display.');

  readonly resolvedTotal = computed(
    () =>
      this.total() ?? this.items().reduce((sum, item) => sum + item.value, 0)
  );

  readonly hasData = computed(() =>
    this.items().some((item) => item.value > 0)
  );

  readonly series = computed(() => this.items().map((item) => item.value));
  readonly labels = computed(() => this.items().map((item) => item.label));
  readonly colors = computed(() => this.items().map((item) => item.color));

  readonly plotOptions = computed(() => {
    const total = this.resolvedTotal();
    const totalLabel = this.totalLabel();

    return {
      pie: {
        donut: {
          size: '72%',
          labels: {
            show: true,
            name: {
              show: true,
              color: 'var(--muted-foreground)',
              fontSize: '13px',
              font: 'var(--default-font-family)',
            },
            value: {
              show: true,
              color: 'var(--foreground)',
              fontSize: '28px',
              fontWeight: 700,
              font: 'var(--default-font-family)',
              formatter: (value: string) => Number(value).toLocaleString(),
            },
            // Resting state (nothing hovered) shows the grand total in the
            // centre; hovering a slice swaps in that slice's name and value.
            total: {
              show: true,
              showAlways: true,
              label: totalLabel,
              color: 'var(--foreground)',
              fontSize: '13px',
              font: 'var(--default-font-family)',
              formatter: () => total.toLocaleString(),
            },
          },
        },
      },
    };
  });

  readonly chart = {
    type: 'donut' as const,
    height: 200,
    width: 200,
    animations: { enabled: false },
    background: 'transparent',
  };

  readonly stroke = { width: 0 };
  readonly legend = { show: false };
  readonly dataLabels = { enabled: false };
  readonly tooltip = {
    theme: 'dark',
    y: { formatter: (value: number) => value.toLocaleString() },
  };
}
