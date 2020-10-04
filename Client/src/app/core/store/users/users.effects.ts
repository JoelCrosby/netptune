import { Injectable } from '@angular/core';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import {
  catchError,
  map,
  switchMap,
  withLatestFrom,
  tap,
} from 'rxjs/operators';
import * as actions from './users.actions';
import { UsersService } from './users.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmationService } from '@core/services/confirmation.service';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';

@Injectable()
export class UsersEffects {
  loadUsers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadUsers),
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
      switchMap(([_, workspace]) =>
        this.usersService.getUsersInWorkspace(workspace.slug).pipe(
          map((users) => actions.loadUsersSuccess({ users })),
          catchError((error) => of(actions.loadUsersFail({ error })))
        )
      )
    )
  );

  onWorkspaceSelected$ = createEffect(() =>
    this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState))
  );

  inviteUsers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.inviteUsersToWorkspace),
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
      switchMap(([{ emailAddresses }, workspace]) =>
        this.usersService
          .inviteUsersToWorkspace(emailAddresses, workspace.slug)
          .pipe(
            tap(() => this.snackbar.open('Invite(s) Sent')),
            map(() =>
              actions.inviteUsersToWorkspaceSuccess({ emailAddresses })
            ),
            catchError((error) =>
              of(actions.inviteUsersToWorkspaceFail({ error }))
            )
          )
      )
    )
  );

  removeUsers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.removeUsersFromWorkspace),
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
      switchMap(([{ emailAddresses }, workspace]) =>
        this.confirmation.open(REMOVE_USERS_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });
            return this.usersService
              .removeUsersFromWorkspace(emailAddresses, workspace.slug)
              .pipe(
                tap(() => this.snackbar.open('User(s) removed')),
                map(() =>
                  actions.removeUsersFromWorkspaceSuccess({ emailAddresses })
                ),
                catchError((error) =>
                  of(actions.removeUsersFromWorkspaceFail({ error }))
                )
              );
          })
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private usersService: UsersService,
    private snackbar: MatSnackBar,
    private confirmation: ConfirmationService,
    private store: Store
  ) {}
}

const REMOVE_USERS_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Remove User(s)',
  color: 'warn',
  title: 'Remove users from workspace',
  message:
    'This will remove the user(s) from the workspace, but will not remove thier accounts.',
};
