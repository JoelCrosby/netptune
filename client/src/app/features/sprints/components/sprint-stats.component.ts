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
      <app-sprint-progress-summary
        [sprint]="sprint()"
        [stats]="resolvedStats()" />
    </section>
  `,
})
export class SprintStatsComponent {
  readonly sprint = input.required<SprintDetailViewModel>();
  readonly stats = input<readonly StatStripItem[] | null>(null);

  protected readonly resolvedStats = computed(() => {
    return this.stats() ?? taskCountStats(this.sprint());
  });
}

function taskCountStats(sprint: SprintDetailViewModel): StatStripItem[] {
  const archived: StatStripItem[] = sprint.archivedTaskCount
    ? [
        {
          label: $localize`:Stat label for archived tasks in a sprint:Archived`,
          value: sprint.archivedTaskCount,
        },
      ]
    : [];

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
    ...archived,
  ];
}
