import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import { StatComponent } from '@static/components/stat/stat.component';

@Component({
  selector: 'app-sprint-stats',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ProgressBarComponent, StatComponent],
  template: `
    <div class="flex flex-col gap-3">
      <div class="grid gap-3 md:grid-cols-4">
        <app-stat label="Total" [value]="sprint().taskCount" />
        <app-stat label="New" [value]="sprint().newTaskCount" />
        <app-stat label="In Progress" [value]="sprint().activeTaskCount" />
        <app-stat label="Complete" [value]="sprint().doneTaskCount" />
      </div>

      @if (sprint().taskCount > 0) {
        <div>
          <app-progress-bar [value]="progressPercent()" />
          <p class="text-muted mt-1 text-right text-xs">
            {{ sprint().doneTaskCount }} / {{ sprint().taskCount }} complete
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
