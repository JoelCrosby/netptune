import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Params } from '@angular/router';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import { taskColumns } from '@core/tasks/task-columns';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { TaskTableComponent } from '@static/components/task-table.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { debounceTime } from 'rxjs/operators';

export interface SprintAddTaskDialogData {
  sprintId: number;
  projectId: number;
}

@Component({
  selector: 'app-sprint-add-task-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormInputComponent,
    TaskTableComponent,
  ],
  template: `
    <app-dialog-title
      i18n="Title of the dialog for adding existing tasks to a sprint">
      Add Tasks to Sprint
    </app-dialog-title>

    <div class="flex w-220 max-w-full flex-col gap-4">
      <app-form-input
        name="sprint-add-task-search"
        i18n-placeholder="Placeholder in the box for finding tasks to add"
        placeholder="Search tasks by name, key or tag"
        [noMargin]="true"
        [value]="searchInput()"
        (valueChange)="searchInput.set($event)" />

      <app-task-table
        containerClass="h-[420px] overflow-y-auto overflow-x-hidden"
        key="sprint-add-tasks"
        url="api/tasks"
        tableClass="table-fixed"
        i18n-emptyMessage="Shown when no tasks match the add-to-sprint search"
        emptyMessage="No tasks available to add."
        [columns]="columns"
        [params]="params"
        [selection]="true"
        [stickyHeader]="true"
        (selectionChanged)="selected.set($event)" />
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">
        <span i18n="Dismisses a dialog without acting">Cancel</span>
      </button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="selected().length === 0 || loading()"
        (click)="add()">
        <ng-container i18n="Button that adds the selected tasks to the sprint">
          {selected().length, plural,
            =0 {Add tasks}
            =1 {Add 1 task}
            other {Add {{ selected().length }} tasks}
          }
        </ng-container>
      </button>
    </div>
  `,
})
export class SprintAddTaskDialogComponent {
  private dialogRef =
    inject<DialogRef<SprintAddTaskDialogComponent>>(DialogRef);
  private dialogData = inject<SprintAddTaskDialogData>(DIALOG_DATA);

  private readonly sprintCommands = inject(SprintCommandsService);

  readonly loading = this.sprintCommands.isUpdating;

  readonly searchInput = signal('');
  readonly selected = signal<readonly TaskViewModel[]>([]);

  // Debounce so each keystroke doesn't trigger a server fetch.
  private search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  // excludeSprintId returns tasks in the project that aren't already in this
  // sprint — the same set the old inline dropdown offered.
  readonly params = computed<Params>(() => {
    const search = this.search().trim();

    return {
      projectId: this.dialogData.projectId,
      excludeSprintId: this.dialogData.sprintId,
      ...(search ? { search } : {}),
    };
  });

  readonly columns = taskColumns<TaskViewModel>(
    ['systemId', 'name', 'status'],
    { overrides: { name: { cellClass: 'min-w-0' } } }
  );

  add() {
    const taskIds = this.selected()
      .map((task) => task.id)
      .filter((id): id is number => id != null);

    if (taskIds.length === 0) return;

    this.sprintCommands.addTasks(this.dialogData.sprintId, { taskIds });

    this.dialogRef.close();
  }

  close() {
    this.dialogRef.close();
  }
}
