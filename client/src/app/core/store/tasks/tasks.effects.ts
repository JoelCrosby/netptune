import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
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
import { clearState } from '../activity/activity.actions';
import * as SprintActions from '../sprints/sprints.actions';
import { selectSelectedSprintFilterId } from '../sprints/sprints.selectors';
import * as TagActions from '../tags/tags.actions';
import { selectSelectedTags } from '../tags/tags.selectors';
import { loadUsers } from '../users/users.actions';
import {
  buildTaskFilterRouteParams,
  parseTaskFilterRouteParams,
} from './task-filter-route-params';
import * as actions from './tasks.actions';
import { ProjectTasksHubService } from './tasks.hub.service';
import {
  selectProjectTasksFilter,
  selectSelectedAssignees,
  selectSelectedTaskStatuses,
  selectTaskSearchTerm,
  selectTasksPage,
  selectTasksPageSize,
} from './tasks.selectors';
import { ProjectTasksService } from './tasks.service';

@Injectable()
export class ProjectTasksEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private projectTasksService = inject(ProjectTasksService);
  private projectTasksHubService = inject(ProjectTasksHubService);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(SnackbarService);
  private store = inject(Store);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loadProjectTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadProjectTasks.init),
      concatLatestFrom(() => [
        this.store.select(selectProjectTasksFilter),
        this.store.select(selectTasksPage),
        this.store.select(selectTasksPageSize),
      ]),
      switchMap(([_, taskFilter, page, pageSize]) =>
        this.projectTasksService.get({ ...taskFilter, page, pageSize }).pipe(
          unwrapClientReposne(),
          map((page) =>
            actions.loadProjectTasks.success({
              tasks: page.items,
              page: page.page,
              pageSize: page.pageSize,
              totalCount: page.totalCount,
              totalPages: page.totalPages,
            })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadProjectTasks.fail({ error }))
          )
        )
      )
    );
  });

  reloadProjectTasksOnPaginationChange$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.setProjectTasksPage, actions.setProjectTasksPageSize),
      map(() => actions.loadProjectTasks.init())
    );
  });

  updateProjectTasksFilter$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(
        actions.setSearchTerm,
        actions.toggleSelectedStatus,
        actions.toggleSelectedAssignee,
        TagActions.toggleSelectedTag,
        SprintActions.setSprintTaskFilter
      ),
      concatLatestFrom(() => [
        this.store.select(selectTaskSearchTerm),
        this.store.select(selectSelectedTags),
        this.store.select(selectSelectedAssignees),
        this.store.select(selectSelectedTaskStatuses),
        this.store.select(selectSelectedSprintFilterId),
        this.store.select(RouteSelectors.selectIsTaskListRoute),
        this.route.queryParamMap,
      ]),
      filter(([action, , , , , , isTaskListRoute, paramMap]) => {
        if (!isTaskListRoute) return false;

        if (action.type === SprintActions.setSprintTaskFilter.type) {
          return (
            parseTaskFilterRouteParams(paramMap).sprintId !== action.sprintId
          );
        }

        return true;
      }),
      map(([, term, tags, users, statuses, sprintId]) =>
        buildTaskFilterRouteParams(
          {
            term,
            tags,
            users,
            statuses,
            sprintId,
          },
          { includeStatuses: true }
        )
      ),
      map((params) => actions.updateProjectTasksFilter({ params }))
    );
  });

  onUpdateProjectTasksFilter$ = createEffect(
    () => {
      return this.actions$.pipe(
        ofType(actions.updateProjectTasksFilter),
        switchMap(({ params }) =>
          this.router.navigate([], {
            queryParams: params,
          })
        )
      );
    },
    { dispatch: false }
  );

  onTaskListRouterNavigated$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(ROUTER_NAVIGATED),
      concatLatestFrom(() => [
        this.store.select(RouteSelectors.selectIsTaskListRoute),
        this.route.queryParamMap,
      ]),
      filter(([, isTaskListRoute]) => isTaskListRoute),
      switchMap(([, , paramMap]) => {
        const filters = parseTaskFilterRouteParams(paramMap);

        return of(
          TagActions.setSelectedTags({ selectedTags: filters.tags ?? [] }),
          actions.hydrateProjectTaskFiltersFromRoute({
            term: filters.term ?? null,
            assigneeIds: filters.users ?? [],
            statuses: filters.statuses ?? [],
            tags: filters.tags ?? [],
            sprintId: filters.sprintId,
          }),
          SprintActions.setSprintTaskFilter({ sprintId: filters.sprintId }),
          actions.loadProjectTasks.init()
        );
      })
    );
  });

  createProjectTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createProjectTask.init),
      switchMap((action) =>
        this.projectTasksHubService.post(action.identifier, action.task).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Task created')),
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
        this.projectTasksHubService.put(action.identifier, action.task).pipe(
          unwrapClientReposne(),
          tap(() => !!action.silent && this.snackbar.open('Task updated')),
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
        this.projectTasksHubService
          .bulkUpdate(action.identifier, action.request)
          .pipe(
            unwrapClientReposne(),
            tap(() => {
              this.snackbar.open('Tasks updated');
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

            return this.projectTasksHubService
              .delete(action.identifier, action.task)
              .pipe(
                unwrapClientReposne(),
                tap(() => this.snackbar.open('Task deleted')),
                map(() => {
                  const taskId = action.task.id;

                  if (taskId === undefined || taskId === null) {
                    throw new Error('taskid was null or undefined');
                  }

                  return actions.deleteProjectTask.success({
                    taskId,
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

              return this.projectTasksHubService
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

  loadTaskDetailUsers$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadTaskDetails.init),
      map(() => loadUsers.init())
    );
  });

  clearTaskDetail$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.clearTaskDetail),
      map(() => clearState())
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

  importTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.importTasks.init),
      switchMap((action) =>
        this.projectTasksService
          .import(action.boardIdentifier, action.file)
          .pipe(
            unwrapClientReposne(),
            tap(() => this.snackbar.open('Import Successful')),
            map(() => actions.importTasks.success()),
            catchError((error) => {
              this.snackbar.open('Import Failed');
              return of(actions.importTasks.fail({ error }));
            })
          )
      )
    );
  });

  addTagToTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.addTagToTask.init),
      switchMap(({ identifier, request }) =>
        this.projectTasksHubService.addTagToTask(identifier, request).pipe(
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
        this.projectTasksHubService
          .deleteTagFromTask(identifier, { systemId, tag })
          .pipe(
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
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this task?',
  title: 'Delete Task',
  color: 'warn',
};

const buildDeleteTasksConfirmation = (count: number): ConfirmDialogOptions => ({
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
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
