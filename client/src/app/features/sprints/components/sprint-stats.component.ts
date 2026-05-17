import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { CardComponent } from '@static/components/card/card.component';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';

@Component({
  selector: 'app-sprint-stats',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, ProgressBarComponent],
  template: `
    <div class="bg-board-group flex flex-col gap-3 p-2">
      <div class="grid gap-3 md:grid-cols-4">
        <app-card class="min-h-0! p-4!">
          <div class="text-muted mb-2 text-sm">Total</div>
          <div class="text-2xl font-semibold">{{ sprint().taskCount }}</div>
        </app-card>
        <app-card class="min-h-0! p-4!">
          <div class="text-muted mb-2 text-sm">New</div>
          <div class="text-2xl font-semibold">{{ sprint().newTaskCount }}</div>
        </app-card>
        <app-card class="min-h-0! p-4!">
          <div class="text-muted mb-2 text-sm">In Progress</div>
          <div class="text-2xl font-semibold">
            {{ sprint().activeTaskCount }}
          </div>
        </app-card>
        <app-card class="min-h-0! p-4!">
          <div class="text-muted mb-2 text-sm">Complete</div>
          <div class="text-2xl font-semibold">{{ sprint().doneTaskCount }}</div>
        </app-card>
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
