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
import { TaskEstimate } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import {
  UserSelectOption,
  UserSelectValue,
} from '@core/models/view-models/user-select-option';
import { taskDetailResource } from '@core/resources/task.resource';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { SprintsService } from '@core/services/sprints.service';
import { StorageService } from '@core/services/storage.service';
import { CurrentTaskService } from '@core/services/current-task.service';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { EditorUpload } from '@static/components/editor/editor.component';
import { catchError, EMPTY, firstValueFrom, tap } from 'rxjs';

@Injectable()
export class TaskDetailService {
  private readonly sprintsService = inject(SprintsService);
  private readonly snackbar = inject(SnackbarService);
  private readonly taskCommands = inject(TaskCommandsService);
  private readonly currentTask = inject(CurrentTaskService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly assistant = inject(AiAssistantService);
  private readonly storage = inject(StorageService);

  private readonly openSystemId = signal<string | undefined>(undefined);

  private readonly resource = taskDetailResource(this.openSystemId);

  readonly task = computed(() => {
    return this.resource.hasValue() ? this.resource.value() : undefined;
  });

  readonly taskId = computed(() => this.task()?.id);
  readonly systemId = computed(() => this.task()?.systemId);
  readonly projectId = computed(() => this.task()?.projectId);

  readonly loading = this.resource.isLoading;
  readonly isEditing = this.taskCommands.isEditing;

  readonly loadError = computed(() => {
    return this.resource.error() as HttpErrorResponse | undefined;
  });

  readonly canAskAssistant = computed(() => {
    return this.assistant.isAvailable() && this.task() !== null;
  });

  // The resource is torn down before this service's destroy hook runs, so the task
  // it held is gone by then; remember what was published to clear it on close.
  private publishedSystemId: string | undefined;

  constructor() {
    effect(() => this.publishCurrentTask());
    inject(DestroyRef).onDestroy(() => this.clearCurrentTask());
  }

  private publishCurrentTask() {
    const task = this.task();

    this.publishedSystemId = task?.systemId;
    this.currentTask.set(task);
  }

  private clearCurrentTask() {
    const systemId = this.publishedSystemId;

    if (!systemId) return;

    this.currentTask.clearIfCurrent(systemId);
  }

  show(systemId: string) {
    this.openSystemId.set(systemId);
  }

  reload() {
    this.resource.reload();
  }

  updateTask(
    update: Partial<UpdateProjectTaskRequest>,
    options?: { silent?: boolean; refresh?: boolean }
  ) {
    const task = this.task();

    if (!task) return;

    this.taskCommands.update(
      { id: task.id, ...update },
      {
        ...options,
        onUpdated: (updated) => this.resource.value.set(updated),
      }
    );
  }

  updateDescription(description: string) {
    this.updateTask({ description }, { silent: true, refresh: false });
  }

  // media embedded in the description is linked to the task so it shows in its files.
  uploadMedia(file: File): Promise<EditorUpload | null> {
    const systemId = this.task()?.systemId ?? null;

    return firstValueFrom(
      this.storage.uploadMedia(file, systemId).pipe(unwrapClientResponse())
    );
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

  askAssistant() {
    const task = this.task();

    if (!task) return;

    this.assistant.askAboutTask(task);
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
