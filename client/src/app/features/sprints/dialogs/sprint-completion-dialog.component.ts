import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { SprintStatus } from '@core/enums/sprint-status';
import { StatusCategory } from '@core/models/status';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { SprintsService } from '@core/store/sprints/sprints.service';
import { completeSprintWithReassignment } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { SelectableCardComponent } from '@static/components/selectable-card/selectable-card.component';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';

type MoveMode = 'backlog' | 'sprint';

@Component({
  selector: 'app-sprint-completion-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    SelectableCardComponent,
    BadgeComponent,
  ],
  template: `
    <app-dialog-title i18n="Title of the dialog for completing a sprint">
      Complete Sprint
    </app-dialog-title>

    <div class="flex flex-col gap-4">
      @if (incompleteTasks().length > 0) {
        <p class="text-muted text-sm">
          <ng-container
            i18n="Count of unfinished tasks when completing a sprint">
            {incompleteTasks().length, plural,
              =1 {
                <strong class="text-foreground">1</strong>
                incomplete task in this sprint.
              }
              other {
                <strong class="text-foreground">
                  {{ incompleteTasks().length }}
                </strong>
                incomplete tasks in this sprint.
              }
            }
          </ng-container>
        </p>

        <div class="border-border max-h-48 overflow-y-auto rounded-md border">
          @for (task of incompleteTasks(); track task.id) {
            <div
              class="border-border flex items-center gap-3 border-b px-3 py-2 last:border-0">
              <span class="text-muted w-16 shrink-0 text-xs font-medium">
                {{ task.systemId }}
              </span>
              <span class="flex-1 truncate text-sm">{{ task.name }}</span>
              <app-badge
                shape="rounded"
                [class]="
                  'shrink-0 px-1.5 ' + statusBadgeClass(task.statusCategory)
                ">
                {{ task.statusName }}
              </app-badge>
            </div>
          }
        </div>

        <div class="flex flex-col gap-2">
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
            [selected]="moveMode() === 'backlog'"
            (selectionChange)="moveMode.set('backlog')">
            <div>
              <p class="text-sm font-medium">
                <span
                  i18n="Option that returns unfinished tasks to the backlog">
                  Move to backlog
                </span>
              </p>
              <p class="text-muted text-xs">
                <span i18n="Explains the move-to-backlog option">
                  Unassign tasks from this sprint
                </span>
              </p>
            </div>
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
            <div>
              <p class="text-sm font-medium">
                <span
                  i18n="
                    Option that moves unfinished tasks into a different sprint
                  ">
                  Move to another sprint
                </span>
              </p>
              <p class="text-muted text-xs">
                <span i18n="Explains the move-to-another-sprint option">
                  Add tasks to an upcoming sprint
                </span>
              </p>
            </div>
          </app-selectable-card>

          @if (moveMode() === 'sprint') {
            @if (planningSprints().length > 0) {
              <app-form-select
                i18n-label="
                  Label of the field choosing which sprint to move tasks into
                "
                label="Target sprint"
                i18n-placeholder="Placeholder in the target sprint picker"
                placeholder="Select sprint"
                [value]="targetSprintId() ?? null"
                (changed)="targetSprintId.set($event)">
                @for (sprint of planningSprints(); track sprint.id) {
                  <app-form-select-option [value]="sprint.id!">
                    {{ sprint.name }}
                  </app-form-select-option>
                }
              </app-form-select>
            } @else {
              <p class="text-muted text-sm">
                <span
                  i18n="
                    Shown when there is no future sprint to move tasks into
                  ">
                  No upcoming sprints available.
                </span>
              </p>
            }
          }
        </div>
      } @else {
        <p class="text-muted text-sm">
          <span i18n="Shown when a sprint has no unfinished tasks left">
            All tasks in this sprint are complete. Ready to close out the
            sprint.
          </span>
        </p>
      }
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
  `,
})
export class SprintCompletionDialogComponent {
  private store = inject(Store);
  private sprintsService = inject(SprintsService);

  dialogRef = inject<DialogRef<SprintCompletionDialogComponent>>(DialogRef);
  sprint = inject<SprintDetailViewModel>(DIALOG_DATA);

  readonly updateLoading = this.store.selectSignal(selectSprintUpdateLoading);
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

  readonly confirmDisabled = computed(
    () =>
      this.updateLoading() ||
      (this.moveMode() === 'sprint' &&
        this.incompleteTasks().length > 0 &&
        (this.planningSprints().length === 0 || !this.targetSprintId()))
  );

  statusBadgeClass(status: StatusCategory): string {
    switch (status) {
      case StatusCategory.todo:
        return 'bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-300';
      case StatusCategory.active:
        return 'bg-yellow-100 text-yellow-700 dark:bg-yellow-500/15 dark:text-yellow-300';
      case StatusCategory.backlog:
        return 'bg-purple-100 text-purple-700 dark:bg-purple-500/15 dark:text-purple-300';
      case StatusCategory.done:
        return 'bg-green-100 text-green-700 dark:bg-green-500/15 dark:text-green-300';
      default:
        return 'bg-neutral-100 text-neutral-600 dark:bg-neutral-500/15 dark:text-neutral-300';
    }
  }

  onConfirm() {
    if (!this.sprint.id) return;

    const incompleteTaskIds = this.incompleteTasks().map((t) => t.id);
    const targetSprintId =
      this.moveMode() === 'sprint' && incompleteTaskIds.length > 0
        ? (this.targetSprintId() ?? undefined)
        : undefined;

    this.store.dispatch(
      completeSprintWithReassignment({
        sprintId: this.sprint.id,
        incompleteTaskIds,
        targetSprintId,
      })
    );

    this.dialogRef.close();
  }
}
