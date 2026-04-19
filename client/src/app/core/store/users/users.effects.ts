import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { EMPTY, of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';
import * as actions from './users.actions';
import { UsersService } from './users.service';

@Injectable()
export class UsersEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private usersService = inject(UsersService);
  private snackbar = inject(SnackbarService);
  private confirmation = inject(ConfirmationService);

  loadUsers$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadUsers),
      switchMap(() =>
        this.usersService.getUsersInWorkspace().pipe(
          map((users) => actions.loadUsersSuccess({ users })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadUsersFail({ error }))
          )
        )
      )
    );
  });

  loadUser$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadUser),
      switchMap(({ userId }) =>
        this.usersService.getUser(userId).pipe(
          map((user) => actions.loadUserSuccess({ user })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadUserFail({ error }))
          )
        )
      )
    );
  });

  onWorkspaceSelected$ = createEffect(() => {
    return this.actions$.pipe(ofType(selectWorkspace), map(actions.clearState));
  });

  inviteUsers$ = createEffect(() => {
    return this.actions$.pipe(
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
    );
  });

  toggleUserPermission$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.toggleUserPermission),
      switchMap(({ userId, permission }) =>
        this.usersService.toggleUserPermission(userId, permission).pipe(
          tap(() => this.snackbar.open('Permission updated')),
          map(() => actions.toggleUserPermissionSuccess({ userId, permission })),
          catchError((error) =>
            of(actions.toggleUserPermissionFail({ error }))
          )
        )
      )
    );
  });

  removeUsers$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.removeUsersFromWorkspace),
      switchMap(({ emailAddresses }) =>
        this.confirmation.open(REMOVE_USERS_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return EMPTY;
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
    );
  });
}

const REMOVE_USERS_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Remove User(s)',
  color: 'warn',
  title: 'Remove users from workspace',
  message:
    'This will remove the user(s) from the workspace, but will not remove thier accounts.',
};
