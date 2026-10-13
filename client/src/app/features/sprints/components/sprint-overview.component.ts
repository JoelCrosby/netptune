import { formatDate } from '@angular/common';
import { Component, LOCALE_ID, computed, inject, input } from '@angular/core';
import { EstimateType, estimateTypeUnits } from '@core/enums/estimate-type';
import { SprintStatus } from '@core/enums/sprint-status';
import { SprintBurndownReport } from '@core/models/reporting';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { MeterEntry } from '@static/components/meter-list/meter-list.component';
import {
  ProgressMarker,
  ProgressPanelComponent,
  ProgressSegment,
  TrendBadge,
} from '@static/components/progress-panel/progress-panel.component';
import { StatStripItem } from '@static/components/stat-strip/stat-strip.component';
import { todaySignal } from '@core/util/today';
import {
  SprintHealth,
  SprintPace,
  sprintHealth,
  startOfDay,
} from '../sprint-health';
import { isOpenTask } from '../sprint-task-filters';
import { SprintBurndownCardComponent } from './sprint-burndown-card.component';
import { SprintWorkloadCardComponent } from './sprint-workload-card.component';

const paces: Record<SprintPace, TrendBadge> = {
  ahead: {
    label: $localize`:Chip shown when a sprint is finishing work faster than time passes:Ahead of pace`,
    tone: 'good',
    direction: 'up',
  },
  onTrack: {
    label: $localize`:Chip shown when a sprint's progress keeps up with time:On pace`,
    tone: 'muted',
    direction: 'flat',
  },
  behind: {
    label: $localize`:Chip shown when a sprint's progress lags behind time:Behind pace`,
    tone: 'bad',
    direction: 'down',
  },
};

interface WorkloadTally {
  entry: Omit<MeterEntry, 'percent' | 'label'>;
  count: number;
  estimate: number;
}

@Component({
  selector: 'app-sprint-overview',
  imports: [
    ProgressPanelComponent,
    SprintBurndownCardComponent,
    SprintWorkloadCardComponent,
  ],
  host: { class: 'flex flex-col gap-6' },
  template: `
    <app-progress-panel
      [value]="health().progressPercent"
      [valueLabel]="valueLabel"
      [caption]="caption()"
      [total]="sprint().taskCount"
      [segments]="segments()"
      [marker]="marker()"
      [badge]="pace()"
      [stats]="stats()" />

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
      @if (burndown(); as report) {
        <app-sprint-burndown-card
          [points]="report.points"
          [start]="start()"
          [end]="end()"
          [today]="today()"
          [velocity]="health().velocity"
          [stats]="scopeStats()" />
      }

      <app-sprint-workload-card
        [class]="burndown() ? '' : 'lg:col-span-2'"
        [entries]="workload()"
        [stats]="workloadStats()" />
    </div>
  `,
})
export class SprintOverviewComponent {
  readonly sprint = input.required<SprintDetailViewModel>();
  readonly burndown = input<SprintBurndownReport | undefined>();

  private readonly locale = inject(LOCALE_ID);
  protected readonly today = todaySignal();

  protected readonly start = computed(() => {
    const sprint = this.sprint();

    return startOfDay(sprint.startedAt ?? sprint.startDate);
  });

  protected readonly end = computed(() => startOfDay(this.sprint().endDate));

  protected readonly health = computed(() => {
    return sprintHealth(this.sprint(), this.burndown(), this.today());
  });

  protected readonly pace = computed(() => {
    const pace = this.health().pace;

    return pace ? paces[pace] : null;
  });

  protected readonly valueLabel = $localize`:Follows the sprint completion percentage:complete`;

  protected readonly caption = computed(() => {
    const { doneTaskCount, taskCount } = this.sprint();

    return $localize`:Sprint task progress. DONE is finished tasks and TOTAL the total:${doneTaskCount}:DONE: / ${taskCount}:TOTAL: tasks`;
  });

  protected readonly marker = computed<ProgressMarker | null>(() => {
    const percent = this.health().timePercent;

    if (percent === null) return null;

    return {
      percent,
      label: $localize`:Marker on the sprint progress bar. PERCENT is time elapsed:Today · ${percent}:PERCENT:% of time`,
    };
  });

  protected readonly scopeStats = computed<StatStripItem[]>(() => {
    const report = this.burndown();

    if (!report) return [];

    return scopeStats(
      report.committedCount,
      report.addedCount,
      report.removedCount
    );
  });

  protected readonly workloadStats = computed<StatStripItem[]>(() => {
    const open = this.sprint().tasks.filter(isOpenTask);
    const unassigned = open.filter((task) => !task.assignees.length).length;
    const shared = open.filter((task) => task.assignees.length > 1).length;

    return [
      {
        label: $localize`:Stat label for open sprint tasks:Open`,
        value: open.length,
      },
      {
        label: $localize`:Label for the number of open tasks with no assignee:Unassigned`,
        value: unassigned,
      },
      {
        label: $localize`:Label for tasks assigned to more than one person:Shared`,
        value: shared,
      },
    ];
  });

  protected readonly segments = computed<ProgressSegment[]>(() => {
    const sprint = this.sprint();
    const visibleDone = sprint.tasks.filter((task) => !isOpenTask(task)).length;
    const archivedDone = Math.max(0, sprint.doneTaskCount - visibleDone);
    const newCount =
      sprint.taskCount - sprint.doneTaskCount - sprint.activeTaskCount;

    return [
      {
        label: $localize`:Legend for finished sprint tasks:Complete`,
        count: visibleDone,
        color: 'primary',
      },
      {
        label: $localize`:Legend for finished sprint tasks that were archived:Complete & archived`,
        count: archivedDone,
        color: 'faint',
      },
      {
        label: $localize`:Legend for sprint tasks being worked on:In progress`,
        count: sprint.activeTaskCount,
        color: 'info',
      },
      {
        label: $localize`:Legend for sprint tasks not started:New`,
        count: Math.max(0, newCount),
        color: 'empty',
      },
    ];
  });

  protected readonly stats = computed<StatStripItem[]>(() => {
    const sprint = this.sprint();

    switch (sprint.status) {
      case SprintStatus.active:
        return this.activeStats(this.health(), sprint);
      case SprintStatus.completed:
        return [
          {
            label: $localize`:Stat label for when a sprint was completed:Completed`,
            value: this.formatDay(sprint.completedAt ?? sprint.endDate),
          },
        ];
      default:
        return [
          {
            label: $localize`:Stat label for when a planned sprint starts:Starts`,
            value: this.formatDay(sprint.startDate),
          },
        ];
    }
  });

  protected readonly workload = computed<MeterEntry[]>(() => {
    const sprint = this.sprint();
    const tallies = tallyWorkload(sprint.tasks.filter(isOpenTask));
    const largest = Math.max(1, ...tallies.map((tally) => tally.count));
    const unit = numericEstimateUnit(sprint.estimateType);

    return tallies.map((tally) => {
      const estimate =
        unit !== null && tally.estimate ? ` · ${tally.estimate}${unit}` : '';

      return {
        ...tally.entry,
        percent: (tally.count / largest) * 100,
        label: `${tally.count}${estimate}`,
      };
    });
  });

  private activeStats(
    health: SprintHealth,
    sprint: SprintDetailViewModel
  ): StatStripItem[] {
    return [
      {
        label: $localize`:Stat label for tasks finished per day:Velocity`,
        value: formatRate(health.velocity),
        suffix: $localize`:Unit after a per-day rate:/day`,
        note:
          health.requiredVelocity === null
            ? undefined
            : $localize`:Velocity needed to finish the sprint on time. RATE is tasks per day:Needs ${formatRate(health.requiredVelocity)}:RATE: to finish`,
      },
      {
        label: $localize`:Stat label for the projected sprint finish date:Projected finish`,
        value: health.projectedFinish
          ? this.formatDay(health.projectedFinish)
          : '—',
        ...finishNote(health.daysEarly),
      },
      {
        label: $localize`:Stat label for tasks being worked on:In progress`,
        value: `${sprint.activeTaskCount}`,
        note: sprint.activeTaskCount
          ? undefined
          : $localize`:Shown when no sprint task is being worked on:Nothing picked up`,
        noteTone: 'warn',
      },
    ];
  }

  private formatDay(value: string | Date): string {
    return formatDate(value, 'd MMM', this.locale);
  }
}

function scopeStats(
  committed: number,
  added: number,
  removed: number
): StatStripItem[] {
  const net = added - removed;
  const percent = committed
    ? Math.round((net / committed) * 100)
    : Math.sign(net) * 100;

  return [
    {
      label: $localize`:Scope committed when the sprint started:Committed`,
      value: committed,
    },
    {
      label: $localize`:Scope added after the sprint started:Added`,
      value: `+${added}`,
    },
    {
      label: $localize`:Scope removed after the sprint started:Removed`,
      value: `−${removed}`,
    },
    {
      label: $localize`:Heading of the sprint scope card:Scope change`,
      value: percent > 0 ? `+${percent}%` : `${percent}%`,
      note: scopeVerdict(percent),
      noteTone: percent > 0 ? 'warn' : 'muted',
    },
  ];
}

function scopeVerdict(percent: number): string {
  if (percent > 0) {
    return $localize`:Shown when work was added to a sprint after it started:scope creep`;
  }

  if (percent < 0) {
    return $localize`:Shown when work was removed from a sprint after it started:scope reduced`;
  }

  return $localize`:Shown when a sprint's scope is unchanged since it started:no change`;
}

function formatRate(rate: number | null): string {
  return rate === null ? '—' : rate.toFixed(1);
}

function finishNote(daysEarly: number | null): Partial<StatStripItem> {
  if (daysEarly === null) {
    return {
      note: $localize`:Shown when no sprint task has been finished yet:No pace yet`,
      noteTone: 'muted',
    };
  }

  if (daysEarly > 0) {
    return {
      note: $localize`:Projected sprint finish ahead of its end date. DAYS is a count:${daysEarly}:DAYS: days early`,
      noteTone: 'good',
    };
  }

  if (daysEarly < 0) {
    return {
      note: $localize`:Projected sprint finish after its end date. DAYS is a count:${-daysEarly}:DAYS: days late`,
      noteTone: 'bad',
    };
  }

  return {
    note: $localize`:Projected sprint finish on its end date:On the end date`,
    noteTone: 'muted',
  };
}

function numericEstimateUnit(
  type: EstimateType | null | undefined
): string | null {
  const isNumeric =
    type === EstimateType.storyPoints || type === EstimateType.hours;

  return isNumeric ? estimateTypeUnits[type] : null;
}

function tallyWorkload(tasks: readonly TaskViewModel[]): WorkloadTally[] {
  const tallies = new Map<string, WorkloadTally>();
  const unassigned: WorkloadTally = {
    entry: {
      key: '',
      name: $localize`:Workload row for open tasks with nobody assigned:Unassigned`,
      placeholder: true,
      tone: 'warn',
    },
    count: 0,
    estimate: 0,
  };

  for (const task of tasks) {
    const estimate = task.estimateValue ?? 0;

    if (!task.assignees.length) {
      unassigned.count++;
      unassigned.estimate += estimate;
    }

    for (const assignee of task.assignees) {
      const tally = tallies.get(assignee.id) ?? {
        entry: {
          key: assignee.id,
          name: assignee.displayName,
          pictureUrl: assignee.pictureUrl,
          isServiceAccount: assignee.isServiceAccount,
        },
        count: 0,
        estimate: 0,
      };

      tally.count++;
      tally.estimate += estimate;
      tallies.set(assignee.id, tally);
    }
  }

  const assigned = [...tallies.values()].sort((a, b) => b.count - a.count);

  return unassigned.count ? [...assigned, unassigned] : assigned;
}
