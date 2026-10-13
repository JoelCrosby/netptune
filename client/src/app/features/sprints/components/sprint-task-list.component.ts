import { Component, computed, inject, input, signal } from '@angular/core';
import { Params } from '@angular/router';
import { SprintStatus } from '@core/enums/sprint-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { DialogService } from '@core/services/dialog.service';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import { taskColumns, taskNameCell } from '@core/tasks/task-columns';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { LucideListMinus } from '@lucide/angular';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { ChipRowComponent } from '@static/components/chip-row/chip-row.component';
import { CountChipComponent } from '@static/components/count-chip/count-chip.component';
import {
  DatatableColumn,
  DatatableMenuItem,
} from '@static/components/datatable/datatable.types';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { TaskEstimateComponent } from '@static/components/task-estimate.component';
import { TaskNameBadge } from '@static/components/task-name.component';
import { TaskTableComponent } from '@static/components/task-table.component';
import { SignalTone } from '@static/components/tones';
import {
  SprintTaskFilter,
  matchesSprintTaskFilter,
  sprintTaskFilterParams,
} from '../sprint-task-filters';

interface AttentionItem {
  filter: SprintTaskFilter;
  label: string;
  tone: SignalTone;
}

const attentionItems: AttentionItem[] = [
  {
    filter: 'highPriority',
    label: $localize`:Attention chip for open high and critical priority tasks:critical / high still open`,
    tone: 'bad',
  },
  {
    filter: 'unassigned',
    label: $localize`:Attention chip for open tasks with nobody assigned:unassigned`,
    tone: 'warn',
  },
  {
    filter: 'noEstimate',
    label: $localize`:Attention chip for open tasks without an estimate:missing estimates`,
    tone: 'warn',
  },
  {
    filter: 'added',
    label: $localize`:Attention chip for tasks added after the sprint started:added mid-sprint`,
    tone: 'muted',
  },
];

const tabOptions: SegmentedOption<SprintTaskFilter>[] = [
  {
    value: 'all',
    label: $localize`:Sprint task filter showing every task:All`,
  },
  {
    value: 'open',
    label: $localize`:Sprint task filter for unfinished tasks:Open`,
  },
  {
    value: 'highPriority',
    label: $localize`:Sprint task filter for high and critical priority tasks:High priority`,
  },
  {
    value: 'unassigned',
    label: $localize`:Sprint task filter for tasks with nobody assigned:Unassigned`,
  },
];

const archivableFilters: readonly SprintTaskFilter[] = ['all', 'open'];

const noTaskIds: ReadonlySet<number> = new Set();

const addedBadge: TaskNameBadge = {
  label: $localize`:Badge on a task added after its sprint started:Added mid-sprint`,
  color: 'pending',
};

const estimateColumn: DatatableColumn<TaskViewModel> = {
  id: 'estimate',
  header: $localize`:Column heading for the task estimate:Estimate`,
  visibleOnMobile: false,
  accessor: 'estimateValue',
  sortable: true,
  sortKey: 'estimateValue',
  widthClass: 'w-24',
  cell: {
    component: TaskEstimateComponent,
    inputs: (task) => ({
      estimateType: task.estimateType,
      estimateValue: task.estimateValue,
    }),
  },
};

@Component({
  selector: 'app-sprint-task-list',
  imports: [
    CheckboxComponent,
    ChipRowComponent,
    CountChipComponent,
    SegmentedControlComponent,
    TaskTableComponent,
  ],
  host: { class: 'flex flex-col gap-4' },
  template: `
    @if (attention().length) {
      <app-chip-row
        i18n-label="Label before the sprint's needs-attention filters"
        label="Needs attention">
        @for (item of attention(); track item.filter) {
          <button
            app-count-chip
            [count]="item.count"
            [tone]="item.tone"
            [active]="filter() === item.filter"
            (click)="toggleFilter(item.filter)">
            {{ item.label }}
          </button>
        }
      </app-chip-row>
    }

    <app-task-table
      key="sprint-tasks"
      url="api/tasks"
      tableClass="md:min-w-[820px] table-fixed"
      [emptyMessage]="emptyMessage()"
      [columns]="columns"
      [params]="params"
      [menu]="menu()"
      [stickyHeader]="true">
      <app-segmented-control
        datatableToolbar
        i18n-ariaLabel="Accessible label of the sprint task filters"
        ariaLabel="Filter sprint tasks"
        [options]="tabs"
        [value]="filter()"
        (valueChange)="selectedFilter.set($event)" />

      @if (sprint().archivedTaskCount) {
        <app-checkbox
          datatableToolbar
          class="ml-auto"
          density="compact"
          [checked]="showArchived()"
          [disabled]="!archivedApplies()"
          (changed)="showArchived.set($event)">
          <span
            i18n="Toggle that shows archived sprint tasks. COUNT is a number">
            Show archived ({{ sprint().archivedTaskCount }})
          </span>
        </app-checkbox>
      }
    </app-task-table>
  `,
})
export class SprintTaskListComponent {
  readonly sprint = input.required<SprintDetailViewModel>();
  readonly addedTaskIds = input<ReadonlySet<number>>(new Set());
  readonly canManage = input.required<boolean>();

  private readonly sprintCommands = inject(SprintCommandsService);
  private readonly dialog = inject(DialogService);

  protected readonly tabs = tabOptions;
  protected readonly selectedFilter = signal<SprintTaskFilter>('all');
  protected readonly showArchived = signal(false);

  // A chip-only filter has no tab to show it is active, so once its chip
  // disappears the table falls back to every task rather than a hidden filter.
  protected readonly filter = computed<SprintTaskFilter>(() => {
    const selected = this.selectedFilter();
    const hasTab = tabOptions.some((option) => option.value === selected);
    const hasChip = this.attention().some((item) => item.filter === selected);

    return hasTab || hasChip ? selected : 'all';
  });

  // Archived tasks never need attention, so only the broad filters include them.
  protected readonly archivedApplies = computed(() => {
    return archivableFilters.includes(this.filter());
  });

  protected readonly params = computed<Params>(() => {
    const filter = this.filter();
    const addedTaskIds = filter === 'added' ? this.addedTaskIds() : noTaskIds;

    return {
      sprintId: this.sprint().id,
      includeArchived: this.showArchived() && this.archivedApplies(),
      ...sprintTaskFilterParams(filter, addedTaskIds),
    };
  });

  protected readonly emptyMessage = computed(() => {
    return this.filter() === 'all'
      ? $localize`:Empty state for the sprint task list:No tasks in this sprint.`
      : $localize`:Shown when no sprint task matches the filter:No tasks match this filter.`;
  });

  protected readonly attention = computed(() => {
    const tasks = this.sprint().tasks;
    const added = this.addedTaskIds();

    return attentionItems
      .map((item) => {
        const count = tasks.filter((task) => {
          return matchesSprintTaskFilter(task, item.filter, added);
        }).length;

        return { ...item, count };
      })
      .filter((item) => item.count > 0);
  });

  protected readonly columns: DatatableColumn<TaskViewModel>[] = [
    ...taskColumns<TaskViewModel>(['systemId', 'name', 'status', 'priority'], {
      overrides: {
        name: taskNameCell<TaskViewModel>({
          action: (task) => this.openTask(task),
          showComments: true,
          archived: (task) => task.isArchived ?? false,
          badges: (task) => {
            return this.addedTaskIds().has(task.id) ? [addedBadge] : [];
          },
        }),
        status: { widthClass: 'w-32 md:w-48' },
      },
    }),
    estimateColumn,
    ...taskColumns<TaskViewModel>(['assignees', 'updatedAt']),
  ];

  private readonly removeMenuItem: DatatableMenuItem<TaskViewModel> = {
    label: $localize`:Row action that removes a task from its sprint:Remove from sprint`,
    icon: LucideListMinus,
    color: 'warn',
    disabled: (task) => task.isArchived ?? false,
    onClick: (task) =>
      this.sprintCommands.removeTask(this.sprint().id, task.id),
  };

  protected readonly menu = computed<DatatableMenuItem<TaskViewModel>[]>(() => {
    const canRemove =
      this.canManage() && this.sprint().status !== SprintStatus.completed;

    return canRemove ? [this.removeMenuItem] : [];
  });

  protected toggleFilter(filter: SprintTaskFilter) {
    const isActive = this.filter() === filter;

    this.selectedFilter.set(isActive ? 'all' : filter);
  }

  private openTask(task: TaskViewModel) {
    this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      height: TaskDetailDialogComponent.height,
      data: task,
      autoFocus: false,
      panelClass: TaskDetailDialogComponent.panelClass,
    });
  }
}
