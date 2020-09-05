import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { BoardGroupType } from '@app/core/models/board-group';
import { ProjectTasksService } from '@app/core/store/tasks/tasks.service';
import {
  isBoardGroupsRoute,
  selectRouterParam,
} from '@core/core.route.selectors';
import { ConfirmationService } from '@core/services/confirmation.service';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import {
  catchError,
  filter,
  map,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import * as actions from './board-groups.actions';
import { selectBoardGroupsSelectedUserIds } from './board-groups.selectors';
import { BoardGroupsService } from './board-groups.service';

@Injectable()
export class BoardGroupsEffects {
  loadBoardGroups$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadBoardGroups, TaskActions.importTasksSuccess),
      withLatestFrom(
        this.store.select(selectRouterParam, 'id'),
        this.route.queryParamMap,
        this.route.queryParams
      ),
      switchMap(([_, id, paramMap, params]) =>
        this.boardGroupsService.get(id, params).pipe(
          map((boardGroups) =>
            actions.loadBoardGroupsSuccess({
              boardGroups,
              selectedIds: paramMap.getAll('users'),
            })
          ),
          catchError((error) => of(actions.loadBoardGroupsFail({ error })))
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

          this.router.navigateByUrl(base);
        })
      ),
    { dispatch: false }
  );

  createBoardGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createBoardGroup),
      switchMap((action) =>
        this.boardGroupsService.post(action.request).pipe(
          map((boardGroup) => actions.createBoardGroupSuccess({ boardGroup })),
          catchError((error) => of(actions.createBoardGroupFail({ error })))
        )
      )
    )
  );

  createProjectTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createProjectTask),
      switchMap((action) =>
        this.projectTasksService.post(action.task).pipe(
          tap(() => this.snackbar.open('Task created')),
          map((task) => actions.createProjectTasksSuccess({ task })),
          catchError((error) => of(actions.createProjectTasksFail({ error })))
        )
      )
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
      withLatestFrom(this.store.select(isBoardGroupsRoute)),
      filter(([_, isCorrectRoute]) => isCorrectRoute),
      map(() => actions.loadBoardGroups())
    )
  );

  deleteBoardGroups$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteBoardGroup),
      switchMap((action) => {
        if (action.boardGroup.type === BoardGroupType.Done) {
          return this.confirmation
            .open({
              isInfoMessage: true,
              message: 'You cannot delete the done column',
            })
            .pipe(switchMap(() => of({ type: 'NO_ACTION' })));
        }

        return this.confirmation.open(DELETE_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.boardGroupsService.delete(action.boardGroup).pipe(
              tap(() => this.snackbar.open('Board Group Deleted')),
              map(() =>
                actions.deleteBoardGroupSuccess({
                  boardGroupId: action.boardGroup.id,
                })
              ),
              catchError((error) => of(actions.deleteBoardGroupFail({ error })))
            );
          })
        );
      })
    )
  );

  editBoardGroups$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.editBoardGroup),
      switchMap((action) =>
        this.boardGroupsService.put(action.boardGroup).pipe(
          map((boardGroup) => actions.editBoardGroupSuccess({ boardGroup })),
          catchError((error) => of(actions.editBoardGroupFail({ error })))
        )
      )
    )
  );

  moveTaskInBoardGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.moveTaskInBoardGroup),
      switchMap((action) =>
        this.boardGroupsService.moveTaskInBoardGroup(action.request).pipe(
          map(actions.moveTaskInBoardGroupSuccess),
          catchError((error) => of(actions.moveTaskInBoardGroupFail({ error })))
        )
      )
    )
  );

  onWorkspaceSelected$ = createEffect(() =>
    this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState))
  );

  toggleUserSelection$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(actions.toggleUserSelection),
        withLatestFrom(this.store.select(selectBoardGroupsSelectedUserIds)),
        tap(async ([_, users]) => {
          await this.router.navigate([], {
            queryParams: { users },
          });
          this.store.dispatch(actions.loadBoardGroups());
        })
      ),
    { dispatch: false }
  );

  constructor(
    private actions$: Actions<Action>,
    private boardGroupsService: BoardGroupsService,
    private projectTasksService: ProjectTasksService,
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
