import { inject, Service } from '@angular/core';
import { AddBoardGroupRequest } from '@core/models/add-board-group-request';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { Status } from '@core/models/status';
import { BoardGroupViewModel } from '@core/models/view-models/board-group-view-model';
import { BoardViewGroup } from '@core/models/view-models/board-view';
import { BoardComposerService } from '@core/services/board-composer.service';
import { BoardSelectionService } from '@core/services/board-selection.service';
import { BoardViewService } from '@core/services/board-view.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { TasksService } from '@core/services/tasks.service';
import { downloadFile } from '@core/util/download-helper';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { MoveMatchingTasksDialogComponent } from '@entry/dialogs/move-matching-tasks-dialog/move-matching-tasks-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, first, forkJoin, switchMap } from 'rxjs';

@Service()
export class BoardGroupCommandsService {
  private readonly tasksApi = inject(TasksService);
  private readonly boardView = inject(BoardViewService);
  private readonly selection = inject(BoardSelectionService);
  private readonly composer = inject(BoardComposerService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly dialog = inject(DialogService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);

  createGroup(request: AddBoardGroupRequest) {
    this.tasksApi
      .addBoardGroup(request)
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe((boardGroup) => {
        this.refresh();
        this.promptMoveMatchingTasks(boardGroup);
      });
  }

  editGroup(request: UpdateBoardGroupRequest) {
    const previousStatusId =
      this.boardView.groups().find((group) => group.id === request.boardGroupId)
        ?.statusId ?? null;

    this.tasksApi
      .putGroup(request)
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe((boardGroup) => {
        this.refresh();

        const statusUnchanged =
          (boardGroup.statusId ?? null) === previousStatusId;

        if (statusUnchanged) return;

        this.promptMoveMatchingTasks(boardGroup);
      });
  }

  deleteGroup(boardGroup: BoardViewGroup) {
    this.confirmation
      .open(DELETE_CONFIRMATION)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.tasksApi.deleteBoardGroup(boardGroup.id);
        }),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Board Group Deleted`
        );
        this.refresh();
      });
  }

  createTask(task: AddProjectTaskRequest) {
    this.tasksApi
      .post({
        ...task,
        assigneeId: this.composer.assigneeId(),
        sprintId: task.sprintId ?? this.composer.sprintId(),
      })
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Task created`
        );
        this.composer.setIsDirty(true);
        this.refresh();
      });
  }

  moveTask(request: MoveTaskInGroupRequest, status?: Status | null) {
    this.boardView.applyTaskMove(request, status);

    this.tasksApi
      .moveTaskInBoardGroup(request)
      .pipe(catchError(() => EMPTY))
      .subscribe();
  }

  moveSelectedTasks(newGroupId: number) {
    this.moveTasksToGroup(newGroupId, this.selection.taskIds());
  }

  moveMatchingTasks(newGroupId: number, taskIds: number[]) {
    this.moveTasksToGroup(newGroupId, taskIds);
  }

  reassignSelectedTasks(assigneeId: string) {
    const identifier = this.boardView.identifier();

    if (!identifier) return;

    this.tasksApi
      .reassignTasks({
        boardId: identifier,
        assigneeId,
        taskIds: this.selection.taskIds(),
      })
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Tasks Re-assigned`
        );
        this.refresh();
      });
  }

  deleteSelectedTasks() {
    const ids = this.selection.taskIds();

    this.confirmation
      .open(DELETE_SELECTED_TASKS_CONFIRMATION)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.tasksApi.deleteMultiple(ids).pipe(unwrapClientResponse());
        }),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Tasks Deleted`
        );
        this.refresh();
      });
  }

  removeSelectedTasksFromBoard() {
    const boardId = this.boardView.board()?.id;
    const taskIds = this.selection.taskIds();

    if (!boardId || !taskIds.length) return;

    this.confirmation
      .open(REMOVE_FROM_BOARD_CONFIRMATION)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          const removals = taskIds.map((taskId) => {
            return this.tasksApi.removeTaskFromBoard(taskId, boardId);
          });

          return forkJoin(removals);
        }),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Tasks removed from board`
        );
        this.selection.clear();
        this.refresh();
      });
  }

  exportTasks() {
    const identifier = this.boardView.identifier();

    if (!identifier) return;

    this.tasksApi
      .export(identifier)
      .pipe(catchError(() => EMPTY))
      .subscribe(
        (response) => void downloadFile(response.file, response.filename)
      );
  }

  private moveTasksToGroup(newGroupId: number, taskIds: number[]) {
    const identifier = this.boardView.identifier();

    if (!identifier) return;

    this.tasksApi
      .moveTasksToGroup({
        boardId: identifier,
        newGroupId,
        taskIds,
      })
      .pipe(
        unwrapClientResponse(),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Tasks Moved`
        );
        this.refresh();
      });
  }

  private promptMoveMatchingTasks(boardGroup: BoardGroupViewModel) {
    if (boardGroup.statusId === null) return;

    const groups = this.boardView.groups();

    const tasks = groups.flatMap((group) => {
      return group.tasks
        .filter((task) => task.statusId === boardGroup.statusId)
        .map((task) => {
          return {
            id: task.id,
            name: task.name,
            systemId: task.systemId,
            groupName: group.name,
          };
        });
    });

    if (tasks.length === 0) return;

    const statusName =
      groups
        .flatMap((group) => group.tasks)
        .find((task) => task.statusId === boardGroup.statusId)?.statusName ??
      '';

    this.dialog
      .open<number[] | undefined>(MoveMatchingTasksDialogComponent, {
        width: MoveMatchingTasksDialogComponent.width,
        data: { groupName: boardGroup.name, statusName, tasks },
      })
      .closed.pipe(first())
      .subscribe((taskIds) => {
        if (!taskIds?.length) return;

        this.moveMatchingTasks(boardGroup.id, taskIds);
      });
  }

  private refresh() {
    this.workspaceRefresh.refresh(['tasks', 'boardGroups']);
  }
}

const DELETE_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Delete`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to delete this group?`,
  title: $localize`:Title of a confirmation dialog:Delete Group`,
};

const REMOVE_FROM_BOARD_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Remove From Board`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to take the selected tasks off this board? They stay on any other board they are on.`,
  title: $localize`:Title of a confirmation dialog:Remove From Board`,
};

const DELETE_SELECTED_TASKS_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Delete Selcted Tasks`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to delete the selected tasks?`,
  title: $localize`:Title of a confirmation dialog:Delete Selected Tasks`,
};
