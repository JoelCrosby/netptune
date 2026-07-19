import { Component, computed, input } from '@angular/core';
import { BurndownPoint } from '@core/models/reporting';

@Component({
  selector: 'app-sprint-burndown-sparkline',
  template: `
    <div class="flex items-center justify-between gap-4">
      <div class="min-w-0 flex-1">
        <p class="text-muted mb-1 text-xs font-medium uppercase">Burndown</p>
        <svg
          class="h-10 w-full"
          viewBox="0 0 100 32"
          preserveAspectRatio="none"
          aria-hidden="true">
          <polyline
            [attr.points]="idealPath()"
            fill="none"
            stroke="var(--muted-foreground)"
            stroke-width="1"
            stroke-dasharray="3 2"
            opacity="0.6"
            vector-effect="non-scaling-stroke" />
          <polyline
            [attr.points]="remainingPath()"
            fill="none"
            stroke="var(--primary)"
            stroke-width="1.5"
            vector-effect="non-scaling-stroke" />
        </svg>
      </div>
      <span
        class="shrink-0 text-xs font-semibold"
        [class]="onTrack() ? 'text-green-600' : 'text-amber-600'">
        {{ caption() }}
      </span>
    </div>
  `,
})
export class SprintBurndownSparklineComponent {
  readonly points = input.required<BurndownPoint[]>();

  private readonly max = computed(() =>
    Math.max(
      1,
      ...this.points().flatMap((point) => [
        point.remaining,
        point.ideal,
        point.totalScope,
      ])
    )
  );

  readonly remainingPath = computed(() =>
    this.toPolyline((point) => point.remaining)
  );
  readonly idealPath = computed(() => this.toPolyline((point) => point.ideal));

  private readonly latestGap = computed(() => {
    const points = this.points();
    const last = points.at(-1);
    return last ? last.remaining - last.ideal : 0;
  });

  readonly onTrack = computed(() => this.latestGap() <= 0.5);

  readonly caption = computed(() =>
    this.onTrack() ? 'On track' : `Behind by ${Math.round(this.latestGap())}`
  );

  private toPolyline(selector: (point: BurndownPoint) => number): string {
    const points = this.points();
    const max = this.max();
    const lastIndex = Math.max(1, points.length - 1);

    return points
      .map((point, index) => {
        const x = (index / lastIndex) * 100;
        const y = 32 - (Math.max(0, selector(point)) / max) * 32;
        return `${x.toFixed(2)},${y.toFixed(2)}`;
      })
      .join(' ');
  }
}
