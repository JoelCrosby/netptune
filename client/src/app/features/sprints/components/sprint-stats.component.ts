import { Component, computed, input } from '@angular/core';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import { StatComponent } from '@static/components/stat/stat.component';

@Component({
  selector: 'app-sprint-stats',
  imports: [ProgressBarComponent, StatComponent],
  template: `
    <div class="flex flex-col gap-6">
      <div class="grid gap-3 md:grid-cols-4">
        <app-stat
          i18n-label="Stat label for the total number of tasks in a sprint"
          label="Total"
          [value]="sprint().taskCount" />
        <app-stat
          i18n-label="Stat label for tasks not started yet"
          label="New"
          [value]="sprint().newTaskCount" />
        <app-stat
          i18n-label="Stat label for tasks being worked on"
          label="In Progress"
          [value]="sprint().activeTaskCount" />
        <app-stat
          i18n-label="Stat label for finished tasks"
          label="Complete"
          [value]="sprint().doneTaskCount" />
      </div>

      @if (sprint().taskCount > 0) {
        <div>
          <app-progress-bar [value]="progressPercent()" />
          <p class="text-foreground mt-2">
            <span
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
            </span>
          </p>
        </div>
      }
    </div>
  `,
})
export class SprintStatsComponent {
  readonly sprint = input.required<SprintDetailViewModel>();

  readonly progressPercent = computed(() => {
    const s = this.sprint();
    if (!s.taskCount) return 0;
    return Math.round((s.doneTaskCount / s.taskCount) * 100);
  });
}
