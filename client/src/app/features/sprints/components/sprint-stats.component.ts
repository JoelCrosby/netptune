import { Component, computed, input } from '@angular/core';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { StatStripItem } from '@static/components/stat-strip/stat-strip.component';
import { SprintProgressSummaryComponent } from './sprint-progress-summary.component';

@Component({
  selector: 'app-sprint-stats',
  imports: [SprintProgressSummaryComponent],
  host: { class: 'block' },
  template: `
    <section
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
      <app-sprint-progress-summary [sprint]="sprint()" [stats]="stats()" />
    </section>
  `,
})
export class SprintStatsComponent {
  readonly sprint = input.required<SprintDetailViewModel>();

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
