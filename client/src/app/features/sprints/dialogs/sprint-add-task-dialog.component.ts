import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Params } from '@angular/router';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { addTasksToSprint } from '@core/store/sprints/sprints.actions';
import { selectSprintUpdateLoading } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { debounceTime } from 'rxjs/operators';
import { SprintBacklogStatusBadgeClassPipe } from '../pipes/sprint-backlog-status-badge-class.pipe';
import { SprintBacklogStatusLabelPipe } from '../pipes/sprint-backlog-status-label.pipe';

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
    DatatableComponent,
    DatatableCellTemplateDirective,
    TaskScopeIdComponent,
    BadgeComponent,
    SprintBacklogStatusBadgeClassPipe,
    SprintBacklogStatusLabelPipe,
  ],
  template: `
    <app-dialog-title>Add Tasks to Sprint</app-dialog-title>

    <div class="flex w-220 max-w-full flex-col gap-4">
      <app-form-input
        name="sprint-add-task-search"
        placeholder="Search tasks by name, key or tag"
        [noMargin]="true"
        [value]="searchInput()"
        (valueChange)="searchInput.set($event)" />

      <app-datatable
        containerClass="h-[420px] overflow-y-auto overflow-x-hidden"
        tableClass="table-fixed"
        rowClass="bg-card"
        emptyMessage="No tasks available to add."
        [data]="data"
        [selection]="true"
        [stickyHeader]="true"
        (selectionChanged)="selected.set($event)">
        <ng-template appDatatableCell="systemId" let-task>
          <app-task-scope-id [id]="task.systemId" />
        </ng-template>

        <ng-template appDatatableCell="name" let-task>
          <span class="block truncate font-medium">{{ task.name }}</span>
        </ng-template>

        <ng-template appDatatableCell="status" let-task>
          <app-badge
            shape="rounded"
            [class]="task.statusCategory | sprintBacklogStatusBadgeClass">
            {{ task.statusName | sprintBacklogStatusLabel }}
          </app-badge>
        </ng-template>
      </app-datatable>
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">Cancel</button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="selected().length === 0 || loading()"
        (click)="add()">
        Add
        {{ selected().length }}
        {{ selected().length === 1 ? 'task' : 'tasks' }}
      </button>
    </div>
  `,
})
export class SprintAddTaskDialogComponent {
  private store = inject(Store);
  private dialogRef =
    inject<DialogRef<SprintAddTaskDialogComponent>>(DialogRef);
  private dialogData = inject<SprintAddTaskDialogData>(DIALOG_DATA);

  readonly loading = this.store.selectSignal(selectSprintUpdateLoading);

  readonly searchInput = signal('');
  readonly selected = signal<readonly TaskViewModel[]>([]);

  // Debounce so each keystroke doesn't trigger a server fetch.
  private search = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(250)),
    { initialValue: '' }
  );

  // excludeSprintId returns tasks in the project that aren't already in this
  // sprint — the same set the old inline dropdown offered.
  private params = computed<Params>(() => {
    const search = this.search().trim();

    return {
      projectId: this.dialogData.projectId,
      excludeSprintId: this.dialogData.sprintId,
      ...(search ? { search } : {}),
    };
  });

  readonly data: DatatableDataSource<TaskViewModel> = {
    key: 'sprint-add-tasks',
    columns: [
      { id: 'systemId', header: 'Key', sortable: true, widthClass: 'w-28' },
      {
        id: 'name',
        header: 'Task',
        accessor: 'name',
        sortable: true,
        cellClass: 'min-w-0',
      },
      { id: 'status', header: 'Status', sortable: true, widthClass: 'w-40' },
    ],
    resource: {
      url: 'api/tasks',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
  };

  add() {
    const taskIds = this.selected()
      .map((task) => task.id)
      .filter((id): id is number => id != null);

    if (taskIds.length === 0) return;

    this.store.dispatch(
      addTasksToSprint({
        sprintId: this.dialogData.sprintId,
        request: { taskIds },
      })
    );

    this.dialogRef.close();
  }

  close() {
    this.dialogRef.close();
  }
}
