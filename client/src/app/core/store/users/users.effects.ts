import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';
import * as actions from './users.actions';
import { UsersService } from './users.service';

@Injectable()
export class UsersEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private usersService = inject(UsersService);
  private snackbar = inject(MatSnackBar);
  private confirmation = inject(ConfirmationService);

  loadUsers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadUsers),
      switchMap(() =>
        this.usersService.getUsersInWorkspace().pipe(
          map((users) => actions.loadUsersSuccess({ users })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadUsersFail({ error }))
          )
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
      switchMap(({ emailAddresses }) =>
        this.usersService.inviteUsersToWorkspace(emailAddresses).pipe(
          tap(() => this.snackbar.open('Invite(s) Sent')),
          map(() => actions.inviteUsersToWorkspaceSuccess({ emailAddresses })),
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
      switchMap(({ emailAddresses }) =>
        this.confirmation.open(REMOVE_USERS_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });
            return this.usersService
              .removeUsersFromWorkspace(emailAddresses)
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
}

const REMOVE_USERS_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Remove User(s)',
  color: 'warn',
  title: 'Remove users from workspace',
  message:
    'This will remove the user(s) from the workspace, but will not remove thier accounts.',
};
