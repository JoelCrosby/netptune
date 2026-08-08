import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import * as RouteSelectors from '@core/core.route.selectors';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { downloadFile } from '@core/util/download-helper';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { ROUTER_NAVIGATED } from '@ngrx/router-store';
import { Action, Store } from '@ngrx/store';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { EMPTY, of } from 'rxjs';
import {
  catchError,
  concatMap,
  filter,
  map,
  switchMap,
  tap,
} from 'rxjs/operators';
import { parseTaskFilterRouteParams } from '@core/router/task-filter-route-params';
import * as actions from './tasks.actions';
import { ProjectTasksFilter } from './tasks.model';
import { SprintFilterService } from '@core/services/sprint-filter.service';
import { ProjectTasksApiService } from './project-tasks-api.service';
import { ProjectTasksHubService } from './tasks.hub.service';

import { ProjectTasksService } from './tasks.service';

@Injectable()
export class ProjectTasksEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private projectTasksService = inject(ProjectTasksService);
  private projectTasksHubService = inject(ProjectTasksHubService);
  private tasksApi = inject(ProjectTasksApiService);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(SnackbarService);
  private store = inject(Store);
  private sprintFilter = inject(SprintFilterService);
  private route = inject(ActivatedRoute);

  loadProjectTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadProjectTasks.init),
      concatLatestFrom(() => [this.route.queryParamMap]),
      switchMap(([_, paramMap]) =>
        this.projectTasksService.get(this.taskFilter(paramMap)).pipe(
          unwrapClientReposne(),
          map((page) =>
            actions.loadProjectTasks.success({ tasks: page.items })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadProjectTasks.fail({ error }))
          )
        )
      )
    );
  });

  /* Everything the list asks for is in the URL, except the sprint, which follows the user. */
  private taskFilter(paramMap: ParamMap): ProjectTasksFilter {
    const filters = parseTaskFilterRouteParams(paramMap);

    return {
      search: filters.term?.trim() || undefined,
      sprintId: this.sprintFilter.sprintId(),
      tags: filters.tags?.length ? filters.tags : undefined,
      statusIds: filters.statuses?.length ? filters.statuses : undefined,
      assignees: filters.users?.length ? filters.users : undefined,
      hasFlags: filters.hasFlags,
      hasTags: filters.hasTags,
    };
  }

  onTaskListRouterNavigated$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(ROUTER_NAVIGATED),
      concatLatestFrom(() => [
        this.store.select(RouteSelectors.selectIsTaskListRoute),
      ]),
      filter(([, isTaskListRoute]) => isTaskListRoute),
      map(() => actions.loadProjectTasks.init())
    );
  });

  createProjectTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createProjectTask.init),
      switchMap((action) =>
        this.tasksApi.post(action.identifier, action.task).pipe(
          unwrapClientReposne(),
          tap(() =>
            this.snackbar.open(
              $localize`:Confirmation shown after an action succeeds:Task created`
            )
          ),
          map((task) => actions.createProjectTask.success({ task })),
          catchError((error: HttpErrorResponse) =>
            of(actions.createProjectTask.fail({ error }))
          )
        )
      )
    );
  });

  editProjectTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.editProjectTask.init),
      concatMap((action) =>
        this.tasksApi.put(action.identifier, action.task).pipe(
          unwrapClientReposne(),
          tap(
            () =>
              !!action.silent &&
              this.snackbar.open(
                $localize`:Confirmation shown after an action succeeds:Task updated`
              )
          ),
          map((task) => actions.editProjectTask.success({ task })),
          catchError((error: HttpErrorResponse) =>
            of(actions.editProjectTask.fail({ error }))
          )
        )
      )
    );
  });

  bulkUpdateTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.bulkUpdateTasks.init),
      concatMap((action) =>
        this.tasksApi.bulkUpdate(action.identifier, action.request).pipe(
          unwrapClientReposne(),
          tap(() => {
            this.snackbar.open(
              $localize`:Confirmation shown after an action succeeds:Tasks updated`
            );
            this.projectTasksHubService.reloadTaskList();
          }),
          map(() => actions.bulkUpdateTasks.success()),
          catchError((error: HttpErrorResponse) =>
            of(actions.bulkUpdateTasks.fail({ error }))
          )
        )
      )
    );
  });

  deleteProjectTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteProjectTask.init),
      switchMap((action) =>
        this.confirmation.open(DELETE_TASK_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return EMPTY;

            return this.tasksApi.delete(action.identifier, action.task).pipe(
              unwrapClientReposne(),
              tap(() =>
                this.snackbar.open(
                  $localize`:Confirmation shown after an action succeeds:Task deleted`
                )
              ),
              map(() => {
                const taskId = action.task.id;
                const identifier = action.identifier;

                if (taskId === undefined || taskId === null) {
                  throw new Error('taskid was null or undefined');
                }

                return actions.deleteProjectTask.success({
                  taskId,
                  identifier,
                });
              }),
              catchError((error) =>
                of(actions.deleteProjectTask.fail({ error }))
              )
            );
          })
        )
      )
    );
  });

  bulkDeleteTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.bulkDeleteTasksAction.init),
      switchMap((action) =>
        this.confirmation
          .open(buildDeleteTasksConfirmation(action.ids.length))
          .pipe(
            switchMap((result) => {
              if (!result) return EMPTY;

              return this.tasksApi
                .deleteMultiple(action.identifier, action.ids)
                .pipe(
                  unwrapClientReposne(),
                  tap(() => {
                    this.snackbar.open(
                      action.ids.length === 1
                        ? 'Task deleted'
                        : `${action.ids.length} tasks deleted`
                    );
                    this.projectTasksHubService.reloadTaskList();
                  }),
                  map(() =>
                    actions.bulkDeleteTasksAction.success({
                      taskIds: action.ids,
                    })
                  ),
                  catchError((error: HttpErrorResponse) =>
                    of(actions.bulkDeleteTasksAction.fail({ error }))
                  )
                );
            })
          )
      )
    );
  });

  loadTaskDetail$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadTaskDetails.init),
      switchMap((action) =>
        this.projectTasksService.detail(action.systemId).pipe(
          map((task) => actions.loadTaskDetails.success({ task })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadTaskDetails.fail({ error }))
          )
        )
      )
    );
  });

  exportTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.exportTasks.init),
      switchMap(() =>
        this.projectTasksService.export().pipe(
          tap((res) => void downloadFile(res.file, res.filename)),
          map((reponse) => actions.exportTasks.success({ reponse })),
          catchError((error: HttpErrorResponse) =>
            of(actions.exportTasks.fail({ error }))
          )
        )
      )
    );
  });

  addTagToTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.addTagToTask.init),
      concatMap(({ identifier, request }) =>
        this.tasksApi.addTagToTask(identifier, request).pipe(
          unwrapClientReposne(),
          map((tag) => actions.addTagToTask.success({ tag })),
          catchError((error: HttpErrorResponse) =>
            of(actions.addTagToTask.fail({ error }))
          )
        )
      )
    );
  });

  deleteTagFromTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteTagFromTask.init),
      switchMap(({ identifier, systemId, tag }) =>
        this.tasksApi.deleteTagFromTask(identifier, { systemId, tag }).pipe(
          unwrapClientReposne(),
          map(() => actions.deleteTagFromTask.success()),
          catchError((error: HttpErrorResponse) =>
            of(actions.deleteTagFromTask.fail({ error }))
          )
        )
      )
    );
  });

  onWorkspaceSelected$ = createEffect(() => {
    return this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState));
  });
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
