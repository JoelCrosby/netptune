import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmationService } from '@core/services/confirmation.service';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { of, asyncScheduler } from 'rxjs';
import { catchError, map, switchMap, tap, debounceTime } from 'rxjs/operators';
import * as actions from './workspaces.actions';
import { WorkspacesService } from './workspaces.service';

@Injectable()
export class WorkspacesEffects {
  loadWorkspaces$ = createEffect(
    ({ debounce = 0, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.loadWorkspaces),
        debounceTime(debounce, scheduler),
        switchMap(() =>
          this.workspacesService.get().pipe(
            map((workspaces) => actions.loadWorkspacesSuccess({ workspaces })),
            catchError((error) => of(actions.loadWorkspacesFail({ error })))
          )
        )
      )
  );

  createWorkspace$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.createWorkspace),
      switchMap((action) =>
        this.workspacesService.post(action.workspace).pipe(
          map((workspace) => actions.createWorkspaceSuccess({ workspace })),
          catchError((error) => of(actions.createWorkspaceFail({ error })))
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
              catchError((error) => of(actions.deleteWorkspaceFail({ error })))
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
          map((workspace) => actions.editWorkspaceSuccess({ workspace })),
          catchError((error) => of(actions.editWorkspaceFail({ error })))
        )
      )
    )
  );

  isSlugUnique$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.isSlugUniue),
      switchMap((action) =>
        this.workspacesService.isSlugUnique(action.slug).pipe(
          map((response) => actions.isSlugUniueSuccess({ response })),
          catchError((error) => of(actions.isSlugUniueFail({ error })))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private workspacesService: WorkspacesService,
    private confirmation: ConfirmationService,
    private snackbar: MatSnackBar
  ) {}
}

const DELETE_WORKSPACE_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this Workspace?',
  title: 'Delete Workspace',
};
