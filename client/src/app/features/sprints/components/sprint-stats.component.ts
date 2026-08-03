import { Component, computed, input } from '@angular/core';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';

@Component({
  selector: 'app-sprint-stats',
  imports: [ProgressBarComponent, StatStripComponent],
  host: { class: 'block' },
  template: `
    <section
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
      <div class="px-6 py-5">
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
              Sprint progress. DONE is the finished task count and TOTAL the
              total
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

        <app-progress-bar class="mt-4 h-2" [value]="progressPercent()" />
      </div>

      <app-stat-strip [items]="stats()" />
    </section>
  `,
})
export class SprintStatsComponent {
  readonly sprint = input.required<SprintDetailViewModel>();

  readonly progressPercent = computed(() => {
    const sprint = this.sprint();

    if (!sprint.taskCount) return 0;

    return Math.round((sprint.doneTaskCount / sprint.taskCount) * 100);
  });

  protected readonly stats = computed<StatStripItem[]>(() => {
    const sprint = this.sprint();

    return [
      {
        label: $localize`:Stat label for the total number of tasks in a sprint:Total`,
        value: sprint.taskCount,
      },
      {
        label: $localize`:Stat label for tasks not started yet:New`,
        value: sprint.newTaskCount,
      },
      {
        label: $localize`:Stat label for tasks being worked on:In Progress`,
        value: sprint.activeTaskCount,
      },
      {
        label: $localize`:Stat label for finished tasks:Complete`,
        value: sprint.doneTaskCount,
      },
    ];
  });
}
