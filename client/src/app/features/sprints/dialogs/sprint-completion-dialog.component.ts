import { DatePipe, formatDate } from '@angular/common';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, LOCALE_ID, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  EstimateType,
  estimateTypeUnits,
  formatEstimate,
} from '@core/enums/estimate-type';
import { SprintStatus } from '@core/enums/sprint-status';
import { StatusCategory } from '@core/models/status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { SprintsService } from '@core/services/sprints.service';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import {
  LucideCalendarClock,
  LucideCalendarRange,
  LucideCheck,
  LucideInbox,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { SelectableCardComponent } from '@static/components/selectable-card/selectable-card.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { SprintDaysBadgeComponent } from '@static/components/sprint-days-badge.component';
import { SprintStatusBadgeComponent } from '@static/components/sprint-status-badge.component';
import { StatStripItem } from '@static/components/stat-strip/stat-strip.component';
import { TaskAssigneesComponent } from '@static/components/task-assignees.component';
import { TaskPriorityComponent } from '@static/components/task-priority.component';
import { TaskStatusPillComponent } from '@static/components/task-status-pill.component';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SprintProgressSummaryComponent } from '../components/sprint-progress-summary.component';

type MoveMode = 'backlog' | 'sprint';

interface CarryOverGroup {
  category: StatusCategory;
  label: string;
  tasks: TaskViewModel[];
}

interface TargetSprintOption {
  id: number;
  label: string;
}

@Component({
  selector: 'app-sprint-completion-dialog',
  imports: [
    DatePipe,
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    IconTileComponent,
    LucideCalendarRange,
    LucideCheck,
    LucideInbox,
    SelectableCardComponent,
    SpinnerComponent,
    SprintDaysBadgeComponent,
    SprintProgressSummaryComponent,
    SprintStatusBadgeComponent,
    TaskAssigneesComponent,
    TaskPriorityComponent,
    TaskStatusPillComponent,
  ],
  template: `
    <div class="relative" [attr.aria-busy]="isCompleting()">
      <div
        class="transition-opacity"
        [class.opacity-40]="isCompleting()"
        [attr.inert]="isCompleting() ? '' : null">
        <app-dialog-title i18n="Title of the dialog for completing a sprint">
          Complete Sprint
        </app-dialog-title>

        <div class="flex flex-col gap-4">
          <div
            class="border-border bg-card flex items-start gap-3 rounded-lg border px-3.5 py-3">
            <app-icon-tile [icon]="sprintIcon" />

            <div class="min-w-0 flex-1">
              <div class="flex flex-wrap items-center gap-2">
                <span class="font-overpass truncate text-base font-semibold">{{
                  sprint.name
                }}</span>
                <app-sprint-status-badge [status]="sprint.status" />
                <app-sprint-days-badge
                  [status]="sprint.status"
                  [endDate]="sprint.endDate" />
              </div>

              <p class="text-muted mt-0.5 text-[13px]">
                <span class="font-medium">{{ sprint.projectName }}</span>
                &nbsp;·&nbsp;
                {{ sprint.startDate | date: 'mediumDate' }} –
                {{ sprint.endDate | date: 'mediumDate' }}
              </p>

              @if (sprint.goal) {
                <p
                  class="text-muted mt-1.5 line-clamp-2 text-[13px] leading-[1.45]">
                  {{ sprint.goal }}
                </p>
              }
            </div>
          </div>

          <div class="border-border bg-card overflow-hidden rounded-lg border">
            <app-sprint-progress-summary
              density="compact"
              [sprint]="sprint"
              [stats]="summaryStats()" />
          </div>

          @if (incompleteTasks().length > 0) {
            <div class="flex flex-col gap-2">
              <p class="text-muted text-[13px]">
                <ng-container
                  i18n="Count of unfinished tasks when completing a sprint">
                  {incompleteTasks().length, plural,
                    =1 {
                      <strong class="text-foreground">1</strong>
                      incomplete task
                    }
                    other {
                      <strong class="text-foreground">
                        {{ incompleteTasks().length }}
                      </strong>
                      incomplete tasks
                    }
                  }
                </ng-container>
                @if (carryOverEstimate(); as remaining) {
                  <span>&nbsp;·&nbsp;{{ remaining }}</span>
                }
              </p>

              <div
                class="border-border custom-scroll max-h-64 overflow-y-auto rounded-md border">
                @for (group of incompleteGroups(); track group.category) {
                  <div
                    class="bg-card-header border-border text-muted sticky top-0 z-10 flex items-center gap-2 border-b px-3 py-1.5 text-[11px] font-semibold tracking-[0.05em] uppercase">
                    {{ group.label }}
                    <span class="text-muted/70"
                      >·&nbsp;{{ group.tasks.length }}</span
                    >
                  </div>

                  @for (task of group.tasks; track task.id) {
                    <div
                      class="border-border grid grid-cols-[56px_1fr_auto_auto_auto] items-center gap-2.5 border-b px-3 py-2.5 last:border-0">
                      <span class="font-avatar text-muted text-[11.5px]">
                        {{ task.systemId }}
                      </span>

                      <span class="min-w-0 truncate text-[13.5px]">
                        {{ task.name }}
                        @if (isOverdue(task)) {
                          <span
                            class="text-[11.5px] text-orange-600 dark:text-orange-300">
                            &nbsp;·&nbsp;<ng-container
                              i18n="Marks a task whose due date has passed"
                              >overdue</ng-container
                            >
                          </span>
                        }
                      </span>

                      @if (task.priority !== null) {
                        <app-task-priority
                          size="small"
                          [priority]="task.priority" />
                      } @else {
                        <span></span>
                      }

                      <span
                        class="font-avatar text-muted text-[11.5px] tabular-nums">
                        {{ estimateLabel(task) }}
                      </span>

                      <span class="flex items-center gap-2">
                        <app-task-status-pill
                          [name]="task.statusName"
                          [color]="task.statusColor"
                          [category]="task.statusCategory" />
                        <app-task-assignees [assignees]="task.assignees" />
                      </span>
                    </div>
                  }
                }
              </div>
            </div>

            <div class="flex flex-col gap-2">
              <p class="text-sm font-medium">
                <span
                  i18n="
                    Asks what to do with unfinished tasks when completing a
                    sprint
                  ">
                  What should happen to these tasks?
                </span>
              </p>

              <app-selectable-card
                groupName="sprint-completion-task-destination"
                i18n-accessibleLabel="
                  Accessible label for the option that returns unfinished tasks
                  to the backlog
                "
                accessibleLabel="Move incomplete tasks to backlog"
                [selected]="moveMode() === 'backlog'"
                (selectionChange)="moveMode.set('backlog')">
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-medium">
                    <span
                      i18n="
                        Option that returns unfinished tasks to the backlog
                      ">
                      Move to backlog
                    </span>
                  </p>
                  <p class="text-muted text-xs">
                    <ng-container
                      i18n="
                        Explains the move-to-backlog option when completing a
                        sprint
                      ">
                      {incompleteTasks().length, plural,
                        =1 {1 task will be unassigned from this sprint}
                        other {
                          {{ incompleteTasks().length }} tasks will be
                          unassigned from this sprint
                        }
                      }
                    </ng-container>
                  </p>
                </div>
                <svg lucideInbox class="text-muted h-4 w-4 shrink-0"></svg>
              </app-selectable-card>

              <app-selectable-card
                groupName="sprint-completion-task-destination"
                i18n-accessibleLabel="
                  Accessible label for the option that moves unfinished tasks to
                  another sprint
                "
                accessibleLabel="Move incomplete tasks to another sprint"
                [selected]="moveMode() === 'sprint'"
                (selectionChange)="moveMode.set('sprint')">
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-medium">
                    <span
                      i18n="
                        Option that moves unfinished tasks into a different
                        sprint
                      ">
                      Move to another sprint
                    </span>
                  </p>
                  <p class="text-muted text-xs">
                    <ng-container
                      i18n="
                        Explains the move-to-another-sprint option when
                        completing a sprint
                      ">
                      {incompleteTasks().length, plural,
                        =1 {1 task will be added to the sprint you pick}
                        other {
                          {{ incompleteTasks().length }} tasks will be added to
                          the sprint you pick
                        }
                      }
                    </ng-container>
                  </p>
                </div>
                <svg
                  lucideCalendarRange
                  class="text-muted h-4 w-4 shrink-0"></svg>
              </app-selectable-card>

              @if (moveMode() === 'sprint') {
                @if (targetSprintOptions().length > 0) {
                  <app-form-select
                    i18n-label="
                      Label of the field choosing which sprint to move tasks
                      into
                    "
                    label="Target sprint"
                    i18n-placeholder="Placeholder in the target sprint picker"
                    placeholder="Select sprint"
                    [value]="targetSprintId() ?? null"
                    (changed)="targetSprintId.set($event)">
                    @for (option of targetSprintOptions(); track option.id) {
                      <app-form-select-option [value]="option.id">
                        {{ option.label }}
                      </app-form-select-option>
                    }
                  </app-form-select>
                } @else {
                  <p class="text-muted text-sm">
                    <span
                      i18n="
                        Shown when there is no future sprint to move tasks into
                      ">
                      No upcoming sprints available. Choose the backlog instead.
                    </span>
                  </p>
                }
              }
            </div>
          } @else {
            <div
              class="border-border text-muted flex items-center gap-2.5 rounded-md border px-3.5 py-3 text-sm">
              <svg
                lucideCheck
                class="h-4 w-4 shrink-0 text-green-600 dark:text-green-400"></svg>
              <ng-container
                i18n="
                  Shown when a sprint has no unfinished tasks left. TOTAL is the
                  task count
                ">
                All
                {{
                  sprint.taskCount // i18n(ph="TOTAL")
                }}
                tasks in this sprint are complete.
              </ng-container>
            </div>
          }

          <p
            class="text-muted border-border border-t pt-3.5 text-xs leading-normal">
            @if (incompleteTasks().length === 0) {
              <ng-container
                i18n="
                  Restates what completing a sprint will do when nothing is left
                  over. SPRINT is the sprint name
                ">
                Completing closes
                {{
                  sprint.name  // i18n(ph="SPRINT")
                }}. Nothing carries over.
              </ng-container>
            } @else if (moveMode() === 'backlog') {
              <ng-container
                i18n="
                  Restates what completing a sprint will do to its unfinished
                  tasks. SPRINT is the sprint name
                ">
                Completing closes
                {{
                  sprint.name // i18n(ph="SPRINT")
                }}
                and returns
                {incompleteTasks().length, plural,
                  =1 {1 task}
                  other {{{ incompleteTasks().length }} tasks}
                }
                to the backlog.
              </ng-container>
            } @else if (targetSprintName(); as targetName) {
              <ng-container
                i18n="
                  Restates what completing a sprint will do to its unfinished
                  tasks. SPRINT is the sprint being closed and TARGET the sprint
                  they move into
                ">
                Completing closes
                {{
                  sprint.name // i18n(ph="SPRINT")
                }}
                and moves
                {incompleteTasks().length, plural,
                  =1 {1 task}
                  other {{{ incompleteTasks().length }} tasks}
                }
                to
                {{
                  targetName  // i18n(ph="TARGET")
                }}.
              </ng-container>
            } @else {
              <ng-container
                i18n="
                  Restates what completing a sprint will do before a target
                  sprint is chosen. SPRINT is the sprint name
                ">
                Completing closes
                {{
                  sprint.name // i18n(ph="SPRINT")
                }}
                and moves
                {incompleteTasks().length, plural,
                  =1 {1 task}
                  other {{{ incompleteTasks().length }} tasks}
                }
                to the sprint you pick.
              </ng-container>
            }
          </p>
        </div>

        <div app-dialog-actions align="end">
          <button app-stroked-button type="button" (click)="dialogRef.close()">
            <span i18n="Dismisses a dialog without acting">Cancel</span>
          </button>
          <button
            app-flat-button
            color="primary"
            type="button"
            [disabled]="confirmDisabled()"
            (click)="onConfirm()">
            <span i18n="Button that completes the sprint">Complete Sprint</span>
          </button>
        </div>
      </div>

      @if (isCompleting()) {
        <div
          class="absolute inset-0 flex flex-col items-center justify-center gap-3">
          <app-spinner diameter="2.5rem" />
          <p
            class="text-muted text-sm"
            i18n="Shown while a sprint is being completed">
            Completing sprint…
          </p>
        </div>
      }
    </div>
  `,
})
export class SprintCompletionDialogComponent {
  private sprintsService = inject(SprintsService);
  private readonly locale = inject(LOCALE_ID);

  dialogRef = inject<DialogRef<SprintCompletionDialogComponent>>(DialogRef);
  sprint = inject<SprintDetailViewModel>(DIALOG_DATA);

  private readonly sprintCommands = inject(SprintCommandsService);

  readonly sprintIcon = LucideCalendarClock;
  readonly updateLoading = this.sprintCommands.isUpdating;
  readonly isCompleting = signal(false);
  readonly moveMode = signal<MoveMode>('backlog');
  readonly targetSprintId = signal<number | null>(null);

  readonly planningSprints = toSignal(
    this.sprintsService
      .get({ status: SprintStatus.planning, projectId: this.sprint.projectId })
      .pipe(catchError(() => of([] as SprintViewModel[]))),
    { initialValue: [] as SprintViewModel[] }
  );

  readonly incompleteTasks = computed(() =>
    this.sprint.tasks.filter((t) => t.statusCategory !== StatusCategory.done)
  );

  readonly incompleteGroups = computed<CarryOverGroup[]>(() => {
    const tasks = this.incompleteTasks();

    return carryOverGroupOrder
      .map((category) => {
        return {
          category,
          label: carryOverGroupLabel(category),
          tasks: tasks.filter((task) => task.statusCategory === category),
        };
      })
      .filter((group) => group.tasks.length > 0);
  });

  // Only story points and hours can be added up; t-shirt sizes are categorical.
  private readonly numericEstimateType = computed(() => {
    const type = this.sprint.estimateType;
    const isNumericUnit =
      type === EstimateType.storyPoints || type === EstimateType.hours;

    return isNumericUnit ? type : null;
  });

  private readonly doneEstimate = computed(() => {
    const doneTasks = this.sprint.tasks.filter((task) => {
      return task.statusCategory === StatusCategory.done;
    });

    return sumEstimates(doneTasks, this.numericEstimateType());
  });

  private readonly remainingEstimate = computed(() => {
    return sumEstimates(this.incompleteTasks(), this.numericEstimateType());
  });

  readonly carryOverEstimate = computed(() => {
    const type = this.numericEstimateType();
    const remaining = this.remainingEstimate();

    if (type === null || remaining === 0) return null;

    const amount = `${remaining}${estimateTypeUnits[type]}`;

    return $localize`:Estimate still open when completing a sprint. AMOUNT is a total such as 26pts:${amount}:AMOUNT: remaining`;
  });

  readonly summaryStats = computed<StatStripItem[]>(() => {
    const stats: StatStripItem[] = [
      {
        label: $localize`:Stat label for finished tasks:Completed`,
        value: this.sprint.doneTaskCount,
      },
      {
        label: $localize`:Stat label for tasks moving out of a sprint being completed:Carrying over`,
        value: this.incompleteTasks().length,
        valueClass: 'text-primary',
      },
      {
        label: $localize`:Stat label for the total number of tasks in a sprint:Total`,
        value: this.sprint.taskCount,
      },
    ];

    const type = this.numericEstimateType();

    if (type !== null) {
      stats.push({
        label: estimateStatLabel(type),
        value: this.doneEstimate(),
        suffix: `/ ${this.doneEstimate() + this.remainingEstimate()}`,
      });
    }

    return stats;
  });

  readonly targetSprintOptions = computed<TargetSprintOption[]>(() => {
    return this.planningSprints().reduce<TargetSprintOption[]>(
      (options, sprint) => {
        if (sprint.id === undefined) return options;

        options.push({ id: sprint.id, label: this.targetSprintLabel(sprint) });

        return options;
      },
      []
    );
  });

  readonly targetSprintName = computed(() => {
    const targetSprintId = this.targetSprintId();

    if (targetSprintId === null) return null;

    const target = this.planningSprints().find((sprint) => {
      return sprint.id === targetSprintId;
    });

    return target?.name ?? null;
  });

  readonly confirmDisabled = computed(
    () =>
      this.updateLoading() ||
      (this.moveMode() === 'sprint' &&
        this.incompleteTasks().length > 0 &&
        (this.targetSprintOptions().length === 0 || !this.targetSprintId()))
  );

  estimateLabel(task: TaskViewModel): string {
    const type = task.estimateType;
    const value = task.estimateValue;

    if (type === null || value === null) return '';

    return formatEstimate(type, value);
  }

  isOverdue(task: TaskViewModel): boolean {
    if (!task.dueDate) return false;

    const startOfToday = new Date();
    startOfToday.setHours(0, 0, 0, 0);

    return new Date(task.dueDate).getTime() < startOfToday.getTime();
  }

  onConfirm() {
    if (!this.sprint.id || this.isCompleting()) return;

    const incompleteTaskIds = this.incompleteTasks().map((t) => t.id);
    const targetSprintId =
      this.moveMode() === 'sprint' && incompleteTaskIds.length > 0
        ? (this.targetSprintId() ?? undefined)
        : undefined;

    this.setCompleting(true);

    this.sprintCommands
      .completeWithReassignment(
        this.sprint.id,
        incompleteTaskIds,
        targetSprintId
      )
      .subscribe({
        next: () => this.dialogRef.close(),
        complete: () => this.setCompleting(false),
      });
  }

  private targetSprintLabel(sprint: SprintViewModel): string {
    const start = formatDate(sprint.startDate, 'mediumDate', this.locale);
    const end = formatDate(sprint.endDate, 'mediumDate', this.locale);
    const tasks =
      sprint.taskCount === 1
        ? $localize`:Task count of a sprint offered as a move target:1 task`
        : $localize`:Task count of a sprint offered as a move target:${sprint.taskCount} tasks`;

    return `${sprint.name} · ${start} – ${end} · ${tasks}`;
  }

  /* Reassigning tasks and closing the sprint are several requests, and none of them can be taken back. */
  private setCompleting(isCompleting: boolean) {
    this.isCompleting.set(isCompleting);
    this.dialogRef.disableClose = isCompleting;
  }
}

const carryOverGroupOrder: StatusCategory[] = [
  StatusCategory.active,
  StatusCategory.todo,
  StatusCategory.new,
  StatusCategory.backlog,
  StatusCategory.inactive,
];

function carryOverGroupLabel(category: StatusCategory): string {
  switch (category) {
    case StatusCategory.active:
      return $localize`:Heading above the in-progress tasks leaving a sprint:In progress`;
    case StatusCategory.todo:
      return $localize`:Heading above the not-started tasks leaving a sprint:To do`;
    case StatusCategory.new:
      return $localize`:Heading above the newly created tasks leaving a sprint:New`;
    case StatusCategory.backlog:
      return $localize`:Heading above the backlog tasks leaving a sprint:Backlog`;
    default:
      return $localize`:Heading above the inactive tasks leaving a sprint:Inactive`;
  }
}

function sumEstimates(
  tasks: TaskViewModel[],
  type: EstimateType | null
): number {
  if (type === null) return 0;

  return tasks.reduce((total, task) => {
    const estimate = task.estimateType === type ? task.estimateValue : null;

    return total + (estimate ?? 0);
  }, 0);
}

function estimateStatLabel(type: EstimateType): string {
  return type === EstimateType.hours
    ? $localize`:Stat label for the hours estimated in a sprint:Hours`
    : $localize`:Stat label for the story points estimated in a sprint:Points`;
}
