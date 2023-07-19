import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatLegacySnackBar as MatSnackBar } from '@angular/material/legacy-snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import * as RouteSelectors from '@core/core.route.selectors';
import { BoardGroupType } from '@core/models/view-models/board-group-view-model';
import { ConfirmationService } from '@core/services/confirmation.service';
import { toggleSelectedTag } from '@core/store/tags/tags.actions';
import { selectSelectedTags } from '@core/store/tags/tags.selectors';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { downloadFile } from '@core/util/download-helper';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of, throwError } from 'rxjs';
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
  loadBoardGroups$ = createEffect(() =>
    this.actions$.pipe(
      ofType(
        actions.loadBoardGroups,
        actions.createBoardGroupSuccess,
        actions.moveSelectedTasksSuccess,
        actions.reassignTasksSuccess,
        actions.editBoardGroupSuccess,
        TaskActions.importTasksSuccess,
        TaskActions.deleteTagFromTaskSuccess,
        TaskActions.addTagToTaskSuccess
      ),
      withLatestFrom(
        this.store.select(RouteSelectors.selectRouterParam, 'id'),
        this.route.queryParamMap,
        this.route.queryParams
      ),
      switchMap(([_, id, paramMap, params]) =>
        this.boardGroupsService.get(id as string, params).pipe(
          unwrapClientReposne(),
          map((boardGroups) =>
            actions.loadBoardGroupsSuccess({
              boardGroups,
              selectedIds: paramMap.getAll('users'),
              onlyFlagged: paramMap.get('flagged') === 'true',
              searchTerm: paramMap.get('term'),
            })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadBoardGroupsFail({ error }))
          )
        )
      )
    )
  );

  loadBoardGroupsFail$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(actions.loadBoardGroupsFail),
        tap(() => {
          const url = this.router.routerState.snapshot.url;
          const parts = url.split('/');
          parts.pop();
          const base = parts.join('/');

          void this.router.navigateByUrl(base);
        })
      ),
    { dispatch: false }
  );

  createBoardGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createBoardGroup),
      switchMap((action) =>
        this.tasksHubService
          .addBoardGroup(action.identifier, action.request)
          .pipe(
            unwrapClientReposne(),
            map((boardGroup) =>
              actions.createBoardGroupSuccess({ boardGroup })
            ),
            catchError((error: HttpErrorResponse) =>
              of(actions.createBoardGroupFail({ error }))
            )
          )
      )
    )
  );

  createProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createProjectTask),
      withLatestFrom(
        this.store.select(selectors.selectBoardIdentifier),
        this.store.select(selectors.selectBoardGroupTaskAssignee)
      ),
      switchMap(([action, identifier, userId]) => {
        if (identifier === undefined) {
          return throwError('board identifier is undefined');
        }

        return this.tasksHubService
          .post(identifier, { ...action.task, assigneeId: userId })
          .pipe(
            unwrapClientReposne(),
            tap(() => this.snackbar.open('Task created')),
            map((task) => actions.createProjectTasksSuccess({ task })),
            catchError((error: HttpErrorResponse) =>
              of(actions.createProjectTasksFail({ error }))
            )
          );
      })
    )
  );

  createProjectTasksSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createProjectTasksSuccess),
      map(() => actions.loadBoardGroups())
    )
  );

  taskDeleted$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TaskActions.deleteProjectTasksSuccess),
      withLatestFrom(this.store.select(RouteSelectors.isBoardGroupsRoute)),
      filter(([_, isCorrectRoute]) => isCorrectRoute),
      map(() => actions.loadBoardGroups())
    )
  );

  deleteBoardGroups$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteBoardGroup),
      switchMap((action) => {
        if (action.boardGroup.type === BoardGroupType.done) {
          return this.confirmation
            .open({
              isInfoMessage: true,
              message: 'You cannot delete the done column',
            })
            .pipe(switchMap(() => of({ type: 'NO_ACTION' })));
        }

        return this.confirmation.open(DELETE_CONFIRMATION).pipe(
          withLatestFrom(this.store.select(selectors.selectBoardIdentifier)),
          switchMap(([result, identifier]) => {
            if (!result) return of({ type: 'NO_ACTION' });

            if (identifier === undefined) {
              return throwError('board identifier is undefined');
            }

            return this.tasksHubService
              .deleteBoardGroup(identifier, action.boardGroup.id)
              .pipe(
                tap(() => this.snackbar.open('Board Group Deleted')),
                map(() =>
                  actions.deleteBoardGroupSuccess({
                    boardGroupId: action.boardGroup.id,
                  })
                ),
                catchError((error) =>
                  of(actions.deleteBoardGroupFail({ error }))
                )
              );
          })
        );
      })
    )
  );

  editBoardGroups$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.editBoardGroup),
      withLatestFrom(this.store.select(selectors.selectBoardIdentifier)),
      switchMap(([action, identifier]) => {
        if (identifier === undefined) {
          return throwError('board identifier is undefined');
        }

        return this.tasksHubService.putGroup(identifier, action.request).pipe(
          unwrapClientReposne(),
          map((boardGroup) => actions.editBoardGroupSuccess({ boardGroup })),
          catchError((error: HttpErrorResponse) =>
            of(actions.editBoardGroupFail({ error }))
          )
        );
      })
    )
  );

  moveTaskInBoardGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.moveTaskInBoardGroup),
      withLatestFrom(this.store.select(selectors.selectBoardIdentifier)),
      switchMap(([action, identifier]) => {
        if (identifier === undefined) {
          return throwError('board identifier is undefined');
        }

        return this.tasksHubService
          .moveTaskInBoardGroup(identifier, action.request)
          .pipe(
            map(actions.moveTaskInBoardGroupSuccess),
            catchError((error) =>
              of(actions.moveTaskInBoardGroupFail({ error }))
            )
          );
      })
    )
  );

  deleteSelectedTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteSelectedTasks),
      switchMap(() =>
        this.confirmation.open(DELETE_SELECTED_TASKS_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.store.select(selectors.selectSelectedTasks).pipe(
              first(),
              map((ids) => actions.deleteTaskMultiple({ ids }))
            );
          })
        )
      )
    )
  );

  deleteTaskMultiple$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteTaskMultiple),
      withLatestFrom(this.store.select(selectors.selectBoardIdentifier)),
      switchMap(([action, identifier]) => {
        if (identifier === undefined) {
          return throwError('board identifier is undefined');
        }

        return this.tasksHubService.deleteMultiple(identifier, action.ids).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Tasks Deleted')),
          map(() => actions.deleteTasksMultipleSuccess()),
          catchError((error: HttpErrorResponse) =>
            of(actions.deleteTasksMultipleFail({ error }))
          )
        );
      })
    )
  );

  deleteTaskMultipleSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteTasksMultipleSuccess),
      map(() => actions.loadBoardGroups())
    )
  );

  moveSelectedTasksToGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.moveSelectedTasks),
      withLatestFrom(
        this.store.select(selectors.selectBoardIdentifier),
        this.store.select(selectors.selectSelectedTasks)
      ),
      switchMap(([action, identifier, taskIds]) => {
        if (identifier === undefined) {
          return throwError('board identifier is undefined');
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
            map(() => actions.moveSelectedTasksSuccess()),
            catchError((error: HttpErrorResponse) =>
              of(actions.moveSelectedTasksFail({ error }))
            )
          );
      })
    )
  );

  onWorkspaceSelected$ = createEffect(() =>
    this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState))
  );

  updateFilters$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(
          actions.toggleUserSelection,
          toggleSelectedTag,
          actions.toggleOnlyFlagged,
          actions.setSearchTerm
        ),
        withLatestFrom(
          this.store.select(selectors.selectBoardGroupsSelectedUserIds),
          this.store.select(selectSelectedTags),
          this.store.select(selectors.selectOnlyFlagged),
          this.store.select(selectors.selectSearchTerm)
        ),
        map(([_, users, tags, flagged, term]) => {
          const usersParam = users?.length ? users : undefined;
          const tagsParam = tags?.length ? tags : undefined;
          const flaggedParam = flagged === true || undefined;
          const termParam = term;

          return [_, usersParam, tagsParam, flaggedParam, termParam];
        }),
        tap(([_, users, tags, flagged, term]) => {
          void this.router
            .navigate([], {
              queryParams: { users, tags, flagged, term },
            })
            .then(() => this.store.dispatch(actions.loadBoardGroups()));
        })
      ),
    { dispatch: false }
  );

  reassignTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.reassignTasks),
      withLatestFrom(
        this.store.select(selectors.selectBoardIdentifier),
        this.store.select(selectors.selectSelectedTasks)
      ),
      switchMap(([action, identifier, taskIds]) => {
        if (identifier === undefined) {
          return throwError('board identifier is undefined');
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
            map(() => actions.reassignTasksSuccess()),
            catchError((error: HttpErrorResponse) =>
              of(actions.reassignTasksFail({ error }))
            )
          );
      })
    )
  );

  exportTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.exportBoardTasks),
      withLatestFrom(this.store.select(selectors.selectBoardIdentifier)),
      switchMap(([_, boardId]) => {
        if (boardId === undefined) {
          return throwError('board identifier is undefined');
        }

        return this.boardGroupsService.export(boardId).pipe(
          tap((res) => void downloadFile(res.file, res.filename)),
          map((reponse) => actions.exportBoardTasksSuccess({ reponse })),
          catchError((error: HttpErrorResponse) =>
            of(actions.exportBoardTasksFail({ error }))
          )
        );
      })
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private boardGroupsService: BoardGroupsService,
    private tasksHubService: ProjectTasksHubService,
    private store: Store,
    private confirmation: ConfirmationService,
    private snackbar: MatSnackBar,
    private router: Router,
    private route: ActivatedRoute
  ) {}
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
