import { inject, Injectable, signal } from '@angular/core';
import { ProjectTask } from '@core/models/project-task';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { BulkUpdateTasksRequest } from '@core/models/requests/bulk-update-tasks-request';
import { DeleteTagFromTaskRequest } from '@core/models/requests/delete-tag-from-task-request';
import { UpdateProjectTaskRequest } from '@core/models/requests/update-project-task-request';
import { ConfirmationService } from '@core/services/confirmation.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { ProjectTasksApiService } from '@core/store/tasks/project-tasks-api.service';
import { downloadFile } from '@core/util/download-helper';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, finalize, switchMap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TaskCommandsService {
  private readonly tasksApi = inject(ProjectTasksApiService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly editing = signal(false);

  readonly isEditing = this.editing.asReadonly();

  create(task: AddProjectTaskRequest) {
    this.tasksApi
      .post(task)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Task created`
        );
        this.refresh();
      });
  }

  update(
    task: Partial<UpdateProjectTaskRequest>,
    options?: { silent?: boolean }
  ) {
    this.editing.set(true);

    this.tasksApi
      .put(task)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY),
        finalize(() => this.editing.set(false))
      )
      .subscribe(() => {
        if (!options?.silent) {
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Task updated`
          );
        }

        this.refresh();
      });
  }

  bulkUpdate(request: BulkUpdateTasksRequest) {
    this.tasksApi
      .bulkUpdate(request)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Tasks updated`
        );
        this.refresh();
      });
  }

  delete(task: ProjectTask, onDeleted?: () => void) {
    this.confirmation
      .open(DELETE_TASK_CONFIRMATION)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.tasksApi.delete(task).pipe(unwrapClientReposne());
        }),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Task deleted`
        );
        this.refresh();
        onDeleted?.();
      });
  }

  deleteMany(ids: number[]) {
    this.confirmation
      .open(buildDeleteTasksConfirmation(ids.length))
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.tasksApi.deleteMultiple(ids).pipe(unwrapClientReposne());
        }),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          ids.length === 1 ? 'Task deleted' : `${ids.length} tasks deleted`
        );
        this.refresh();
      });
  }

  addTag(request: AddTagToTaskRequest) {
    this.tasksApi
      .addTagToTask(request)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY)
      )
      .subscribe(() => this.workspaceRefresh.refresh(['tasks', 'tags']));
  }

  removeTag(request: DeleteTagFromTaskRequest) {
    this.tasksApi
      .deleteTagFromTask(request)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY)
      )
      .subscribe(() => this.workspaceRefresh.refresh(['tasks', 'tags']));
  }

  export() {
    this.tasksApi
      .export()
      .pipe(catchError(() => EMPTY))
      .subscribe(
        (response) => void downloadFile(response.file, response.filename)
      );
  }

  private refresh() {
    this.workspaceRefresh.refresh(['tasks']);
  }
}

const DELETE_TASK_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Delete`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to delete this task?`,
  title: $localize`:Title of a confirmation dialog:Delete Task`,
  color: 'warn',
};

const buildDeleteTasksConfirmation = (count: number): ConfirmDialogOptions => ({
  acceptLabel: $localize`:Confirms the action in a dialog:Delete`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message:
    count === 1
      ? 'Are you sure you want to delete this task?'
      : `Are you sure you want to delete these ${count} tasks?`,
  title: count === 1 ? 'Delete Task' : 'Delete Tasks',
  confirmationCheckboxLabel:
    count === 1
      ? 'I understand this task will be permanently deleted.'
      : `I understand these ${count} tasks will be permanently deleted.`,
  color: 'warn',
});
