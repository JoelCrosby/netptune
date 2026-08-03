import { Component, computed, input } from '@angular/core';
import { colorHex } from '@core/util/colors/colors';
import { numberFormat } from '@core/util/locale';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { NgApexchartsModule } from 'ng-apexcharts';
import { ColorSwatchComponent } from '../color-swatch/color-swatch.component';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { IconTileComponent } from '../icon-tile.component';
import { SkeletonComponent } from '../skeleton/skeleton.component';

export interface DonutStatItem {
  label: string;
  value: number;
  color: string;
}

@Component({
  selector: 'app-donut-stat-card',
  imports: [
    ColorSwatchComponent,
    EmptyStateComponent,
    IconTileComponent,
    LucideDynamicIcon,
    NgApexchartsModule,
    SkeletonComponent,
  ],
  template: `
    <section
      class="border-border bg-card flex h-full min-h-24 flex-col overflow-hidden rounded-lg border shadow-sm">
      <header
        class="border-border flex shrink-0 flex-wrap items-center justify-between gap-x-4 gap-y-2 border-b px-6 py-5">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="icon()" />
          <h3 class="font-overpass truncate text-base font-semibold">
            {{ title() }}
          </h3>
        </div>

        <div class="shrink-0 empty:hidden">
          <ng-content select="[card-actions]" />
        </div>
      </header>

      @if (loading()) {
        <div
          class="flex flex-1 items-center gap-6 px-6 py-5"
          role="status"
          [attr.aria-label]="title()">
          <app-skeleton class="h-40 w-40 shrink-0 rounded-full" />
          <div class="flex flex-1 flex-col gap-4">
            @for (row of skeletonRows; track $index) {
              <div class="flex items-center gap-3">
                <app-skeleton class="h-2.5 w-2.5 shrink-0 rounded-full" />
                <app-skeleton class="h-3 flex-1" />
                <app-skeleton class="h-3 w-8 shrink-0" />
              </div>
            }
          </div>
        </div>
      } @else if (hasData()) {
        <div class="flex flex-1 flex-wrap items-center gap-6 px-6 py-5">
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

          <ul class="flex min-w-40 flex-1 flex-col gap-3">
            @for (item of items(); track item.label) {
              <li class="flex items-center gap-3">
                <app-color-swatch [color]="item.color" />
                <span class="text-muted flex-1 truncate text-sm">
                  {{ item.label }}
                </span>
                <span
                  class="text-foreground shrink-0 text-sm font-semibold tabular-nums">
                  {{ formatValue(item.value) }}
                </span>
                <span
                  class="text-muted w-10 shrink-0 text-right text-xs tabular-nums">
                  {{ formatShare(item.value) }}
                </span>
              </li>
            }
          </ul>
        </div>
      } @else {
        <div class="flex flex-1 items-center justify-center px-6 py-5">
          <app-empty-state compact [title]="emptyMessage()">
            <svg emptyStateIcon [lucideIcon]="icon()" class="h-8 w-8"></svg>
          </app-empty-state>
        </div>
      }
    </section>
  `,
})
export class DonutStatCardComponent {
  /**
   * Bare toLocaleString() follows the browser locale, not the app locale, so all
   * call sites share one app-locale formatter.
   */
  private readonly valueFormat = numberFormat();
  private readonly shareFormat = numberFormat({
    style: 'percent',
    maximumFractionDigits: 0,
  });

  readonly title = input.required<string>();
  readonly icon = input.required<LucideIconInput>();
  readonly items = input<DonutStatItem[]>([]);
  /** Centre figure. Defaults to the sum of all item values when not provided. */
  readonly total = input<number | null>(null);
  readonly totalLabel = input('Total');
  readonly emptyMessage = input('No data to display.');
  readonly loading = input(false);

  protected readonly skeletonRows = Array.from({ length: 4 });

  readonly resolvedTotal = computed(
    () =>
      this.total() ?? this.items().reduce((sum, item) => sum + item.value, 0)
  );

  readonly hasData = computed(() =>
    this.items().some((item) => item.value > 0)
  );

  readonly series = computed(() => this.items().map((item) => item.value));
  readonly labels = computed(() => this.items().map((item) => item.label));
  readonly colors = computed(() =>
    this.items().map((item) => colorHex(item.color))
  );

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
              font: 'var(--font-overpass)',
            },
            value: {
              show: true,
              color: 'var(--foreground)',
              fontSize: '28px',
              fontWeight: 700,
              font: 'var(--font-overpass)',
              formatter: (value: string) => this.formatValue(Number(value)),
            },
            // Resting state (nothing hovered) shows the grand total in the
            // centre; hovering a slice swaps in that slice's name and value.
            total: {
              show: true,
              showAlways: true,
              label: totalLabel,
              color: 'var(--foreground)',
              fontSize: '13px',
              font: 'var(--font-overpass)',
              formatter: () => this.formatValue(total),
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
    y: { formatter: (value: number) => this.formatValue(value) },
  };

  protected formatValue(value: number): string {
    return this.valueFormat.format(value);
  }

  protected formatShare(value: number): string {
    const total = this.resolvedTotal();

    if (!total) return '';

    return this.shareFormat.format(value / total);
  }
}
