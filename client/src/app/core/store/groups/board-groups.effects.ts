import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { ActivatedRoute, Router } from '@angular/router';
import * as RouteSelectors from '@core/core.route.selectors';
import { BoardGroupType } from '@core/models/view-models/board-group-view-model';
import { ConfirmationService } from '@core/services/confirmation.service';
import { toggleSelectedTag } from '@core/store/tags/tags.actions';
import { selectSelectedTags } from '@core/store/tags/tags.selectors';
import * as SprintActions from '@core/store/sprints/sprints.actions';
import { selectSelectedSprintFilterId } from '@core/store/sprints/sprints.selectors';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { downloadFile } from '@core/util/download-helper';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import {
  buildTaskFilterRouteParams,
  parseTaskFilterRouteParams,
} from '@core/store/tasks/task-filter-route-params';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { ROUTER_NAVIGATED } from '@ngrx/router-store';
import { Action, Store } from '@ngrx/store';
import { EMPTY, of, throwError } from 'rxjs';
import {
  catchError,
  filter,
  first,
  map,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import * as actions from './board-groups.actions';
import * as selectors from './board-groups.selectors';
import { BoardGroupsService } from './board-groups.service';

@Injectable()
export class BoardGroupsEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private boardGroupsService = inject(BoardGroupsService);
  private tasksHubService = inject(ProjectTasksHubService);
  private store = inject(Store);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(SnackbarService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loadBoardGroups$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(
        actions.loadBoardGroups.init,
        actions.createBoardGroup.success,
        actions.moveSelectedTasks.success,
        actions.reassignTasks.success,
        actions.editBoardGroup.success,
        TaskActions.importTasks.success,
        TaskActions.deleteTagFromTask.success,
        TaskActions.addTagToTask.success
      ),
      concatLatestFrom(() => [
        this.store.select(RouteSelectors.selectRouterParam('id')),
        this.route.queryParamMap,
        this.store.select(RouteSelectors.selectIsBoardGroupsRoute),
        this.store.select(selectSelectedSprintFilterId),
      ]),
      filter(([, , , isBoardGroupsRoute]) => isBoardGroupsRoute),
      switchMap(([_, id, paramMap, , selectedSprintFilterId]) => {
        const routeFilters = parseTaskFilterRouteParams(paramMap);
        const sprintId = selectedSprintFilterId ?? routeFilters.sprintId;
        const requestParams = buildTaskFilterRouteParams(
          {
            ...routeFilters,
            sprintId,
          },
          { includeStatuses: true }
        );

        return this.boardGroupsService.get(id as string, requestParams).pipe(
          unwrapClientReposne(),
          map((boardGroups) =>
            actions.loadBoardGroups.success({
              boardGroups,
              selectedIds: routeFilters.users ?? [],
              selectedStatusIds: routeFilters.statuses ?? [],
              searchTerm: routeFilters.term ?? null,
              sprintId,
            })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadBoardGroups.fail({ error }))
          )
        );
      })
    );
  });

  loadBoardGroupsFail$ = createEffect(
    () => {
      return this.actions$.pipe(
        ofType(actions.loadBoardGroups.fail),
        tap(() => {
          const url = this.router.routerState.snapshot.url;
          const parts = url.split('/');
          parts.pop();
          const base = parts.join('/');

          void this.router.navigateByUrl(base);
        })
      );
    },
    { dispatch: false }
  );

  createBoardGroup$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createBoardGroup.init),
      switchMap((action) =>
        this.tasksHubService
          .addBoardGroup(action.identifier, action.request)
          .pipe(
            unwrapClientReposne(),
            map((boardGroup) =>
              actions.createBoardGroup.success({ boardGroup })
            ),
            catchError((error: HttpErrorResponse) =>
              of(actions.createBoardGroup.fail({ error }))
            )
          )
      )
    );
  });

  createProjectTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createProjectTask.init),
      concatLatestFrom(() => [
        this.store.select(selectors.selectBoardIdentifier),
        this.store.select(selectors.selectBoardGroupTaskAssignee),
      ]),
      switchMap(([action, identifier, userId]) => {
        if (identifier === undefined) {
          return throwError(() => new Error('board identifier is undefined'));
        }

        return this.tasksHubService
          .post(identifier, { ...action.task, assigneeId: userId })
          .pipe(
            unwrapClientReposne(),
            tap(() => this.snackbar.open('Task created')),
            map((task) => actions.createProjectTask.success({ task })),
            catchError((error: HttpErrorResponse) =>
              of(actions.createProjectTask.fail({ error }))
            )
          );
      })
    );
  });

  createProjectTaskSuccess$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createProjectTask.success),
      concatLatestFrom(() => [
        this.store.select(selectors.selectInlineTaskContent),
      ]),
      map(() => actions.setIsInlineDirty({ isDirty: true }))
    );
  });

  createProjectTasksSuccess$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createProjectTask.success),
      map(() => actions.loadBoardGroups.init())
    );
  });

  taskDeleted$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(TaskActions.deleteProjectTask.success),
      concatLatestFrom(() =>
        this.store.select(RouteSelectors.selectIsBoardGroupsRoute)
      ),
      filter(([_, isCorrectRoute]) => isCorrectRoute),
      map(() => actions.loadBoardGroups.init())
    );
  });

  deleteBoardGroups$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteBoardGroup.init),
      switchMap((action) => {
        if (action.boardGroup.type === BoardGroupType.done) {
          return this.confirmation
            .open({
              isInfoMessage: true,
              message: 'You cannot delete the done column',
            })
            .pipe(switchMap(() => EMPTY));
        }

        return this.confirmation.open(DELETE_CONFIRMATION).pipe(
          withLatestFrom(this.store.select(selectors.selectBoardIdentifier)),
          switchMap(([result, identifier]) => {
            if (!result) return EMPTY;

            if (identifier === undefined) {
              return throwError(
                () => new Error('board identifier is undefined')
              );
            }

            return this.tasksHubService
              .deleteBoardGroup(identifier, action.boardGroup.id)
              .pipe(
                tap(() => this.snackbar.open('Board Group Deleted')),
                map(() =>
                  actions.deleteBoardGroup.success({
                    boardGroupId: action.boardGroup.id,
                  })
                ),
                catchError((error) =>
                  of(actions.deleteBoardGroup.fail({ error }))
                )
              );
          })
        );
      })
    );
  });

  editBoardGroups$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.editBoardGroup.init),
      concatLatestFrom(() =>
        this.store.select(selectors.selectBoardIdentifier)
      ),
      switchMap(([action, identifier]) => {
        if (identifier === undefined) {
          return throwError(() => new Error('board identifier is undefined'));
        }

        return this.tasksHubService.putGroup(identifier, action.request).pipe(
          unwrapClientReposne(),
          map((boardGroup) => actions.editBoardGroup.success({ boardGroup })),
          catchError((error: HttpErrorResponse) =>
            of(actions.editBoardGroup.fail({ error }))
          )
        );
      })
    );
  });

  moveTaskInBoardGroup$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.moveTaskInBoardGroup.init),
      concatLatestFrom(() =>
        this.store.select(selectors.selectBoardIdentifier)
      ),
      switchMap(([action, identifier]) => {
        if (identifier === undefined) {
          return throwError(() => new Error('board identifier is undefined'));
        }

        return this.tasksHubService
          .moveTaskInBoardGroup(identifier, action.request)
          .pipe(
            map(actions.moveTaskInBoardGroup.success),
            catchError((error) =>
              of(actions.moveTaskInBoardGroup.fail({ error }))
            )
          );
      })
    );
  });

  deleteSelectedTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteSelectedTasks),
      switchMap(() =>
        this.confirmation.open(DELETE_SELECTED_TASKS_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return EMPTY;

            return this.store.select(selectors.selectSelectedTasks).pipe(
              first(),
              map((ids) => actions.deleteTaskMultiple.init({ ids }))
            );
          })
        )
      )
    );
  });

  deleteTaskMultiple$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteTaskMultiple.init),
      concatLatestFrom(() =>
        this.store.select(selectors.selectBoardIdentifier)
      ),
      switchMap(([action, identifier]) => {
        if (identifier === undefined) {
          return throwError(() => new Error('board identifier is undefined'));
        }

        return this.tasksHubService.deleteMultiple(identifier, action.ids).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Tasks Deleted')),
          map(() => actions.deleteTaskMultiple.success()),
          catchError((error: HttpErrorResponse) =>
            of(actions.deleteTaskMultiple.fail({ error }))
          )
        );
      })
    );
  });

  deleteTaskMultipleSuccess$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteTaskMultiple.success),
      map(() => actions.loadBoardGroups.init())
    );
  });

  moveSelectedTasksToGroup$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.moveSelectedTasks.init),
      concatLatestFrom(() => [
        this.store.select(selectors.selectBoardIdentifier),
        this.store.select(selectors.selectSelectedTasks),
      ]),
      switchMap(([action, identifier, taskIds]) => {
        if (identifier === undefined) {
          return throwError(() => new Error('board identifier is undefined'));
        }

        return this.tasksHubService
          .moveTasksToGroup(identifier, {
            boardId: identifier,
            newGroupId: action.newGroupId,
            taskIds,
          })
          .pipe(
            unwrapClientReposne(),
            tap(() => this.snackbar.open('Tasks Moved')),
            map(() => actions.moveSelectedTasks.success()),
            catchError((error: HttpErrorResponse) =>
              of(actions.moveSelectedTasks.fail({ error }))
            )
          );
      })
    );
  });

  onWorkspaceSelected$ = createEffect(() => {
    return this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState));
  });

  syncSprintTaskFilterToBoard$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(SprintActions.setSprintTaskFilter),
      concatLatestFrom(() =>
        this.store.select(RouteSelectors.selectIsBoardGroupsRoute)
      ),
      filter(([, isBoardGroupsRoute]) => isBoardGroupsRoute),
      map(([{ sprintId }]) => actions.setSprintFilter({ sprintId }))
    );
  });

  updateFilters$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(
        actions.toggleUserSelection,
        toggleSelectedTag,
        actions.toggleStatusSelection,
        actions.setSearchTerm,
        actions.setSprintFilter
      ),
      concatLatestFrom(() => [
        this.store.select(selectors.selectBoardGroupsSelectedUserIds),
        this.store.select(selectSelectedTags),
        this.store.select(selectors.selectBoardGroupsSelectedStatusIds),
        this.store.select(selectors.selectSearchTerm),
        this.store.select(selectors.selectSelectedSprintId),
        this.store.select(RouteSelectors.selectIsBoardGroupsRoute),
      ]),
      filter(([, , , , , , isBoardGroupsRoute]) => isBoardGroupsRoute),
      map(([_, users, tags, statuses, term, sprintId]) =>
        buildTaskFilterRouteParams(
          {
            users,
            tags,
            statuses,
            term,
            sprintId,
          },
          { includeStatuses: true }
        )
      ),
      map((params) => actions.updateBoardFilter({ params }))
    );
  });

  onUpdateBoardFilters$ = createEffect(
    () => {
      return this.actions$.pipe(
        ofType(actions.updateBoardFilter),
        switchMap(({ params }) =>
          this.router.navigate([], {
            queryParams: params,
          })
        )
      );
    },
    { dispatch: false }
  );

  onRouterNavigated$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(ROUTER_NAVIGATED),
      concatLatestFrom(() => [
        this.store.select(RouteSelectors.selectIsBoardGroupsRoute),
      ]),
      filter(([, isBoardGroupsRoute]) => isBoardGroupsRoute),
      map(() => actions.loadBoardGroups.init())
    );
  });

  reassignTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.reassignTasks.init),
      concatLatestFrom(() => [
        this.store.select(selectors.selectBoardIdentifier),
        this.store.select(selectors.selectSelectedTasks),
      ]),
      switchMap(([action, identifier, taskIds]) => {
        if (identifier === undefined) {
          return throwError(() => new Error('board identifier is undefined'));
        }

        return this.tasksHubService
          .reassignTasks(identifier, {
            boardId: identifier,
            assigneeId: action.assigneeId,
            taskIds,
          })
          .pipe(
            unwrapClientReposne(),
            tap(() => this.snackbar.open('Tasks Re-assigned')),
            map(() => actions.reassignTasks.success()),
            catchError((error: HttpErrorResponse) =>
              of(actions.reassignTasks.fail({ error }))
            )
          );
      })
    );
  });

  exportTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.exportBoardTasks.init),
      concatLatestFrom(() =>
        this.store.select(selectors.selectBoardIdentifier)
      ),
      switchMap(([_, boardId]) => {
        if (boardId === undefined) {
          return throwError(() => new Error('board identifier is undefined'));
        }

        return this.boardGroupsService.export(boardId).pipe(
          tap((res) => void downloadFile(res.file, res.filename)),
          map((reponse) => actions.exportBoardTasks.success({ reponse })),
          catchError((error: HttpErrorResponse) =>
            of(actions.exportBoardTasks.fail({ error }))
          )
        );
      })
    );
  });
}

const DELETE_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this group?',
  title: 'Delete Group',
};

const DELETE_SELECTED_TASKS_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete Selcted Tasks',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete the selected tasks?',
  title: 'Delete Selected Tasks',
};
