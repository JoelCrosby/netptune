import { Component, computed, input } from '@angular/core';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import {
  StatStripComponent,
  StatStripDensity,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';

const headerClasses: Record<StatStripDensity, string> = {
  comfortable: 'px-6 py-5',
  compact: 'px-4 pt-3.5 pb-4',
};

const barClasses: Record<StatStripDensity, string> = {
  comfortable: 'mt-4 h-2',
  compact: 'mt-3 h-2',
};

@Component({
  selector: 'app-sprint-progress-summary',
  imports: [ProgressBarComponent, StatStripComponent],
  host: { class: 'block' },
  template: `
    <div [class]="headerClass()">
      <div class="flex flex-wrap items-baseline justify-between gap-x-4">
        <p
          class="flex items-baseline gap-2"
          i18n="Sprint completion. PERCENT is a whole number">
          <span class="text-3xl font-semibold tracking-tight tabular-nums">
            {{
              progressPercent()  // i18n(ph="PERCENT")
            }}%
          </span>
          complete
        </p>

        <p
          class="text-muted text-sm tabular-nums"
          i18n="
            Sprint progress. DONE is the finished task count and TOTAL the total
          ">
          {{
            sprint().doneTaskCount // i18n(ph="DONE")
          }}
          /
          {{
            sprint().taskCount // i18n(ph="TOTAL")
          }}
          complete
        </p>
      </div>

      <app-progress-bar [class]="barClass()" [value]="progressPercent()" />
    </div>

    <app-stat-strip [items]="stats()" [density]="density()" />
  `,
})
export class SprintProgressSummaryComponent {
  readonly sprint = input.required<SprintDetailViewModel>();
  readonly stats = input.required<readonly StatStripItem[]>();
  readonly density = input<StatStripDensity>('comfortable');

  readonly progressPercent = computed(() => {
    const sprint = this.sprint();

    if (!sprint.taskCount) return 0;

    return Math.round((sprint.doneTaskCount / sprint.taskCount) * 100);
  });

  protected readonly headerClass = computed(
    () => headerClasses[this.density()]
  );

  protected readonly barClass = computed(() => barClasses[this.density()]);
}
