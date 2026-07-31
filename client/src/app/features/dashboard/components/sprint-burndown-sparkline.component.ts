import { Component, computed, input } from '@angular/core';
import { BurndownPoint } from '@core/models/reporting';
import { SprintBurndownMiniChartComponent } from './sprint-burndown-mini-chart.component';

@Component({
  selector: 'app-sprint-burndown-sparkline',
  imports: [SprintBurndownMiniChartComponent],
  host: { class: 'block' },
  template: `
    <div class="mb-1 flex items-center justify-between gap-4">
      <p class="text-muted text-xs font-medium uppercase">
        <span i18n="Heading above the sprint burndown sparkline">Burndown</span>
      </p>
      <span
        class="shrink-0 text-xs font-semibold"
        [class]="
          onTrack()
            ? 'text-green-600 dark:text-green-400'
            : 'text-amber-600 dark:text-amber-400'
        ">
        {{ caption() }}
      </span>
    </div>
    <app-sprint-burndown-mini-chart [points]="points()" />
  `,
})
export class SprintBurndownSparklineComponent {
  readonly points = input.required<BurndownPoint[]>();

  private readonly latestGap = computed(() => {
    const points = this.points();
    const last = points.at(-1);
    return last ? last.remaining - last.ideal : 0;
  });

  readonly onTrack = computed(() => this.latestGap() <= 0.5);

  readonly caption = computed(() => {
    return this.onTrack()
      ? $localize`:Sprint burndown status:On track`
      : $localize`:Sprint burndown status. GAP is the amount behind the ideal line:Behind by ${Math.round(this.latestGap())}:gap:`;
  });
}
