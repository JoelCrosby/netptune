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
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
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
  ],
  template: `
    <app-dialog-title>Complete Sprint</app-dialog-title>

    <div class="flex flex-col gap-4">
      @if (incompleteTasks().length > 0) {
        <p class="text-muted text-sm">
          <strong class="text-foreground">{{
            incompleteTasks().length
          }}</strong>
          incomplete {{ incompleteTasks().length === 1 ? 'task' : 'tasks' }} in
          this sprint.
        </p>

        <div class="border-border max-h-48 overflow-y-auto rounded-md border">
          @for (task of incompleteTasks(); track task.id) {
            <div
              class="border-border flex items-center gap-3 border-b px-3 py-2 last:border-0">
              <span class="text-muted w-16 shrink-0 text-xs font-medium">
                {{ task.systemId }}
              </span>
              <span class="flex-1 truncate text-sm">{{ task.name }}</span>
              <span
                class="shrink-0 rounded px-1.5 py-0.5 text-xs font-medium"
                [class]="statusBadgeClass(task.statusCategory)">
                {{ task.statusName }}
              </span>
            </div>
          }
        </div>

        <div class="flex flex-col gap-2">
          <p class="text-sm font-medium">What should happen to these tasks?</p>

          <button
            type="button"
            class="flex items-center gap-3 rounded-md border px-4 py-3 text-left transition-colors"
            [class]="
              moveMode() === 'backlog'
                ? 'border-primary bg-primary/5'
                : 'border-border hover:border-primary/40'
            "
            (click)="moveMode.set('backlog')">
            <div
              class="flex h-4 w-4 shrink-0 items-center justify-center rounded-full border-2"
              [class]="
                moveMode() === 'backlog' ? 'border-primary' : 'border-border'
              ">
              @if (moveMode() === 'backlog') {
                <div class="bg-primary h-2 w-2 rounded-full"></div>
              }
            </div>
            <div>
              <p class="text-sm font-medium">Move to backlog</p>
              <p class="text-muted text-xs">Unassign tasks from this sprint</p>
            </div>
          </button>

          <button
            type="button"
            class="flex items-center gap-3 rounded-md border px-4 py-3 text-left transition-colors"
            [class]="
              moveMode() === 'sprint'
                ? 'border-primary bg-primary/5'
                : 'border-border hover:border-primary/40'
            "
            (click)="moveMode.set('sprint')">
            <div
              class="flex h-4 w-4 shrink-0 items-center justify-center rounded-full border-2"
              [class]="
                moveMode() === 'sprint' ? 'border-primary' : 'border-border'
              ">
              @if (moveMode() === 'sprint') {
                <div class="bg-primary h-2 w-2 rounded-full"></div>
              }
            </div>
            <div>
              <p class="text-sm font-medium">Move to another sprint</p>
              <p class="text-muted text-xs">Add tasks to an upcoming sprint</p>
            </div>
          </button>

          @if (moveMode() === 'sprint') {
            @if (planningSprints().length > 0) {
              <app-form-select
                label="Target sprint"
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
              <p class="text-muted text-sm">No upcoming sprints available.</p>
            }
          }
        </div>
      } @else {
        <p class="text-muted text-sm">
          All tasks in this sprint are complete. Ready to close out the sprint.
        </p>
      }
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="dialogRef.close()">
        Cancel
      </button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="confirmDisabled()"
        (click)="onConfirm()">
        Complete Sprint
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
        return 'bg-blue-100 text-blue-700';
      case StatusCategory.active:
        return 'bg-yellow-100 text-yellow-700';
      case StatusCategory.backlog:
        return 'bg-purple-100 text-purple-700';
      case StatusCategory.done:
        return 'bg-green-100 text-green-700';
      default:
        return 'bg-neutral-100 text-neutral-600';
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
