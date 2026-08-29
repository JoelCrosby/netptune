import {
  computed,
  DestroyRef,
  effect,
  inject,
  Injectable,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { UpdateProjectTaskRequest } from '@app/core/models/requests/update-project-task-request';
import { taskDetailResource } from '@core/resources/task.resource';
import { SprintsService } from '@core/services/sprints.service';
import { CurrentTaskService } from '@core/services/current-task.service';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, tap } from 'rxjs';

@Injectable()
export class TaskDetailService {
  private readonly sprintsService = inject(SprintsService);
  private readonly snackbar = inject(SnackbarService);
  private readonly taskCommands = inject(TaskCommandsService);
  private readonly currentTask = inject(CurrentTaskService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly openSystemId = signal<string | undefined>(undefined);

  private readonly resource = taskDetailResource(this.openSystemId);

  readonly task = this.resource.value;
  readonly loading = this.resource.isLoading;

  /* The view separates a task that is gone from a request that failed. */
  readonly loadError = computed(
    () => this.resource.error() as HttpErrorResponse | undefined
  );

  constructor() {
    /* The assistant asks what the user is looking at, from outside this view. */
    effect(() => this.currentTask.set(this.task()));

    inject(DestroyRef).onDestroy(() => this.clearCurrentTask());
  }

  private clearCurrentTask() {
    const task = this.task();

    if (!task) return;

    this.currentTask.clearIfCurrent(task.systemId);
  }

  show(systemId: string) {
    this.openSystemId.set(systemId);
  }

  reload() {
    this.resource.reload();
  }

  updateTask(update: Partial<UpdateProjectTaskRequest>) {
    const task = this.task();

    if (!task) return;

    this.taskCommands.update({ ...task, ...update });
  }

  deleteTask(onDeleted?: () => void) {
    const task = this.task();

    if (!task) return;

    this.taskCommands.delete(task, onDeleted);
  }

  assignSprint(sprintId: number) {
    const task = this.task();

    if (!task?.id) return;

    this.sprintsService
      .addTasks(sprintId, { taskIds: [task.id] })
      .pipe(
        unwrapClientResponse(),
        tap(() => {
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Task added to sprint`
          );
          this.workspaceRefresh.refresh(['tasks', 'sprints']);
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }

  clearSprint() {
    const task = this.task();

    if (!task?.id || !task.sprintId) return;

    this.sprintsService
      .removeTask(task.sprintId, task.id)
      .pipe(
        unwrapClientResponse(),
        tap(() => {
          this.snackbar.open(
            $localize`:Confirmation shown after an action succeeds:Task removed from sprint`
          );
          this.workspaceRefresh.refresh(['tasks', 'sprints']);
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }
}
