import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { selectIsAuthenticated } from '@core/auth/store/auth.selectors';
import { AppState } from '@core/core.state';
import { ConfirmationService } from '@core/services/confirmation.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import {
  catchError,
  filter,
  map,
  switchMap,
  tap,
  throttleTime,
  withLatestFrom,
} from 'rxjs/operators';
import { ProjectTasksHubService } from '../tasks/tasks.hub.service';
import * as actions from './workspaces.actions';
import { WorkspacesService } from './workspaces.service';

@Injectable()
export class WorkspacesEffects {
  init$ = createEffect(() =>
    this.actions$.pipe(
      ofType('[WORKSPACES]: Init'),
      withLatestFrom(this.store.select(selectIsAuthenticated)),
      filter(([_, isAuth]) => isAuth),
      map(() => actions.loadWorkspaces())
    )
  );

  selectWorkspace$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(actions.selectWorkspace),
        tap(() => void this.hubService.disconnect())
      ),
    { dispatch: false }
  );

  loadWorkspaces$ = createEffect(
    ({ throttle = 800, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.loadWorkspaces),
        throttleTime(throttle, scheduler),
        switchMap(() =>
          this.workspacesService.get().pipe(
            map((workspaces) => actions.loadWorkspacesSuccess({ workspaces })),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadWorkspacesFail({ error }))
            )
          )
        )
      )
  );

  createWorkspace$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createWorkspace),
      switchMap((action) =>
        this.workspacesService.post(action.workspace).pipe(
          unwrapClientReposne(),
          map((workspace) => actions.createWorkspaceSuccess({ workspace })),
          catchError((error: HttpErrorResponse) =>
            of(actions.createWorkspaceFail({ error }))
          )
        )
      )
    )
  );

  deleteWorkspace$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteWorkspace),
      switchMap(({ workspace }) =>
        this.confirmation.open(DELETE_WORKSPACE_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.workspacesService.delete(workspace).pipe(
              tap(() => this.snackbar.open('Workspace deleted')),
              map(() => actions.deleteWorkspaceSuccess({ workspace })),
              catchError((error: HttpErrorResponse) =>
                of(actions.deleteWorkspaceFail({ error }))
              )
            );
          })
        )
      )
    )
  );

  editWorkspace$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.editWorkspace),
      switchMap((action) =>
        this.workspacesService.put(action.workspace).pipe(
          unwrapClientReposne(),
          map((workspace) => actions.editWorkspaceSuccess({ workspace })),
          catchError((error: HttpErrorResponse) =>
            of(actions.editWorkspaceFail({ error }))
          )
        )
      )
    )
  );

  isSlugUnique$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.isSlugUniue),
      switchMap((action) =>
        this.workspacesService.isSlugUnique(action.slug).pipe(
          unwrapClientReposne(),
          map((response) => actions.isSlugUniueSuccess({ response })),
          catchError((error: HttpErrorResponse) =>
            of(actions.isSlugUniueFail({ error }))
          )
        )
      )
    )
  );

  constructor(
    private store: Store<AppState>,
    private actions$: Actions<Action>,
    private workspacesService: WorkspacesService,
    private confirmation: ConfirmationService,
    private snackbar: MatSnackBar,
    private hubService: ProjectTasksHubService
  ) {}

  ngrxOnInitEffects(): Action {
    return { type: '[WORKSPACES]: Init' };
  }
}

const DELETE_WORKSPACE_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this Workspace?',
  title: 'Delete Workspace',
};
