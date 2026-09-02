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
import { TaskPriority } from '@core/enums/task-priority';
import {
  UserSelectOption,
  UserSelectValue,
} from '@core/models/view-models/user-select-option';
import { TaskEstimate } from '@static/components/task-properties/task-estimate-select.component';
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

  readonly task = computed(() => {
    return this.resource.hasValue() ? this.resource.value() : undefined;
  });

  readonly loading = this.resource.isLoading;
  readonly isEditing = this.taskCommands.isEditing;

  readonly loadError = computed(() => {
    return this.resource.error() as HttpErrorResponse | undefined;
  });

  constructor() {
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

  setStatus(statusId: number | null) {
    if (statusId === null) return;

    this.updateTask({ statusId });
  }

  setPriority(priority: TaskPriority | null) {
    this.updateTask({ priority });
  }

  setEstimate({ estimateType, estimateValue }: TaskEstimate) {
    this.updateTask({ estimateType, estimateValue });
  }

  setStartDate(startDate: string) {
    this.updateTask({ startDate: startDate || null });
  }

  setDueDate(dueDate: string) {
    this.updateTask({ dueDate: dueDate || null });
  }

  setProject(projectId: number | null) {
    if (projectId === null) return;

    this.updateTask({ projectId });
  }

  setSprint(sprintId: number | null) {
    if (sprintId === null) {
      this.clearSprint();

      return;
    }

    this.assignSprint(sprintId);
  }

  setAssignees(assignees: UserSelectValue[]) {
    this.updateTask({ assigneeIds: assignees.map((assignee) => assignee.id) });
  }

  toggleAssignee(user: UserSelectOption) {
    const assignees = this.task()?.assignees ?? [];
    const selected = assignees.some((assignee) => assignee.id === user.id);

    this.setAssignees(
      selected
        ? assignees.filter((assignee) => assignee.id !== user.id)
        : [...assignees, user]
    );
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
