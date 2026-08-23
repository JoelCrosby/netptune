import { formatDate } from '@angular/common';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, LOCALE_ID, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { EstimateType } from '@core/enums/estimate-type';
import { SprintStatus } from '@core/enums/sprint-status';
import { StatusCategory } from '@core/models/status';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { SprintsService } from '@core/services/sprints.service';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import {
  numericEstimateType,
  sumTaskEstimates,
} from '@core/tasks/task-estimates';
import { LucideCalendarRange, LucideCheck, LucideInbox } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { BusyOverlayComponent } from '@static/components/busy-overlay.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { SelectableCardComponent } from '@static/components/selectable-card/selectable-card.component';
import { SprintIdentityComponent } from '@static/components/sprint-identity.component';
import { StatStripItem } from '@static/components/stat-strip/stat-strip.component';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SprintCarryOverListComponent } from '../components/sprint-carry-over-list.component';
import { SprintStatsComponent } from '../components/sprint-stats.component';

type MoveMode = 'backlog' | 'sprint';

interface TargetSprintOption {
  id: number;
  label: string;
}

@Component({
  selector: 'app-sprint-completion-dialog',
  imports: [
    BusyOverlayComponent,
    CalloutComponent,
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    SelectableCardComponent,
    SprintCarryOverListComponent,
    SprintIdentityComponent,
    SprintStatsComponent,
  ],
  template: `
    <app-busy-overlay [busy]="isCompleting()" [message]="completingMessage">
      <app-dialog-title i18n="Title of the dialog for completing a sprint">
        Complete Sprint
      </app-dialog-title>
      <div class="flex flex-col gap-6">
        <app-sprint-identity variant="card" showGoal [sprint]="sprint" />

        <app-sprint-stats [sprint]="sprint" [stats]="summaryStats()" />

        @if (incompleteTasks().length > 0) {
          <app-sprint-carry-over-list
            [tasks]="incompleteTasks()"
            [estimateType]="sprint.estimateType ?? null" />

          <div class="flex flex-col gap-3">
            <p class="text-sm font-medium">
              <span
                i18n="
                  Asks what to do with unfinished tasks when completing a sprint
                ">
                What should happen to these tasks?
              </span>
            </p>

            <app-selectable-card
              groupName="sprint-completion-task-destination"
              i18n-accessibleLabel="
                Accessible label for the option that returns unfinished tasks to
                the backlog
              "
              accessibleLabel="Move incomplete tasks to backlog"
              i18n-heading="Option that returns unfinished tasks to the backlog"
              heading="Move to backlog"
              [icon]="backlogIcon"
              [description]="backlogDescription()"
              [selected]="moveMode() === 'backlog'"
              (selectionChange)="moveMode.set('backlog')" />

            <app-selectable-card
              groupName="sprint-completion-task-destination"
              i18n-accessibleLabel="
                Accessible label for the option that moves unfinished tasks to
                another sprint
              "
              accessibleLabel="Move incomplete tasks to another sprint"
              i18n-heading="
                Option that moves unfinished tasks into a different sprint
              "
              heading="Move to another sprint"
              [icon]="targetSprintIcon"
              [description]="targetSprintDescription()"
              [selected]="moveMode() === 'sprint'"
              (selectionChange)="moveMode.set('sprint')" />

            @if (moveMode() === 'sprint') {
              @if (targetSprintOptions().length > 0) {
                <app-form-select
                  i18n-label="
                    Label of the field choosing which sprint to move tasks into
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
          <app-callout [icon]="allDoneIcon">
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
          </app-callout>
        }

        <p
          class="text-muted border-border border-t pt-5 text-sm leading-normal">
          {{ effectSummary() }}
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
    </app-busy-overlay>
  `,
})
export class SprintCompletionDialogComponent {
  private sprintsService = inject(SprintsService);
  private readonly locale = inject(LOCALE_ID);

  dialogRef = inject<DialogRef<SprintCompletionDialogComponent>>(DialogRef);
  sprint = inject<SprintDetailViewModel>(DIALOG_DATA);

  private readonly sprintCommands = inject(SprintCommandsService);

  readonly completingMessage = $localize`:Shown while a sprint is being completed:Completing sprint\u2026`;

  readonly allDoneIcon = LucideCheck;
  readonly backlogIcon = LucideInbox;
  readonly targetSprintIcon = LucideCalendarRange;

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

    const type = numericEstimateType(this.sprint.estimateType);

    if (type !== null) {
      const done = sumTaskEstimates(this.doneTasks(), type);
      const remaining = sumTaskEstimates(this.incompleteTasks(), type);

      stats.push({
        label: estimateStatLabel(type),
        value: done,
        suffix: `/ ${done + remaining}`,
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

  readonly backlogDescription = computed(() => {
    const count = this.incompleteTasks().length;

    return count === 1
      ? $localize`:Explains the move-to-backlog option when completing a sprint:1 task will be unassigned from this sprint`
      : $localize`:Explains the move-to-backlog option when completing a sprint. COUNT is the number of tasks:${count}:COUNT: tasks will be unassigned from this sprint`;
  });

  readonly targetSprintDescription = computed(() => {
    const count = this.incompleteTasks().length;

    return count === 1
      ? $localize`:Explains the move-to-another-sprint option when completing a sprint:1 task will be added to the sprint you pick`
      : $localize`:Explains the move-to-another-sprint option when completing a sprint. COUNT is the number of tasks:${count}:COUNT: tasks will be added to the sprint you pick`;
  });

  readonly effectSummary = computed(() => {
    const sprintName = this.sprint.name;
    const count = this.incompleteTasks().length;

    if (count === 0) {
      return $localize`:Restates what completing a sprint will do when nothing is left over. SPRINT is the sprint name:Completing closes ${sprintName}:SPRINT:. Nothing carries over.`;
    }

    const tasks = taskCountLabel(count);

    if (this.moveMode() === 'backlog') {
      return $localize`:Restates what completing a sprint will do to its unfinished tasks. SPRINT is the sprint name and TASKS a task count:Completing closes ${sprintName}:SPRINT: and returns ${tasks}:TASKS: to the backlog.`;
    }

    const targetName = this.targetSprintName();

    if (targetName === null) {
      return $localize`:Restates what completing a sprint will do before a target sprint is chosen. SPRINT is the sprint name and TASKS a task count:Completing closes ${sprintName}:SPRINT: and moves ${tasks}:TASKS: to the sprint you pick.`;
    }

    return $localize`:Restates what completing a sprint will do to its unfinished tasks. SPRINT is the sprint being closed, TASKS a task count and TARGET the sprint they move into:Completing closes ${sprintName}:SPRINT: and moves ${tasks}:TASKS: to ${targetName}:TARGET:.`;
  });

  readonly confirmDisabled = computed(
    () =>
      this.updateLoading() ||
      (this.moveMode() === 'sprint' &&
        this.incompleteTasks().length > 0 &&
        (this.targetSprintOptions().length === 0 || !this.targetSprintId()))
  );

  private readonly doneTasks = computed(() =>
    this.sprint.tasks.filter((t) => t.statusCategory === StatusCategory.done)
  );

  private readonly targetSprintName = computed(() => {
    const targetSprintId = this.targetSprintId();

    if (targetSprintId === null) return null;

    const target = this.planningSprints().find((sprint) => {
      return sprint.id === targetSprintId;
    });

    return target?.name ?? null;
  });

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

    return `${sprint.name} · ${start} – ${end} · ${taskCountLabel(sprint.taskCount)}`;
  }

  /* Reassigning tasks and closing the sprint are several requests, and none of them can be taken back. */
  private setCompleting(isCompleting: boolean) {
    this.isCompleting.set(isCompleting);
    this.dialogRef.disableClose = isCompleting;
  }
}

function taskCountLabel(count: number): string {
  return count === 1
    ? $localize`:Task count:1 task`
    : $localize`:Task count. COUNT is the number of tasks:${count}:COUNT: tasks`;
}

function estimateStatLabel(type: EstimateType): string {
  return type === EstimateType.hours
    ? $localize`:Stat label for the hours estimated in a sprint:Hours`
    : $localize`:Stat label for the story points estimated in a sprint:Points`;
}
