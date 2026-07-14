import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { ConfirmationService } from '@core/services/confirmation.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { Action, Store } from '@ngrx/store';
import { EMPTY, of } from 'rxjs';
import { catchError, map, mergeMap, switchMap, tap } from 'rxjs/operators';
import * as actions from './users.actions';
import { UsersService } from './users.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { selectUsersPage, selectUsersPageSize } from './users.selectors';

@Injectable()
export class UsersEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private usersService = inject(UsersService);
  private snackbar = inject(SnackbarService);
  private confirmation = inject(ConfirmationService);
  private store = inject(Store);

  loadUsers$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadUsers.init),
      concatLatestFrom(() => [
        this.store.select(selectUsersPage),
        this.store.select(selectUsersPageSize),
      ]),
      switchMap(([_, page, pageSize]) =>
        this.usersService.getUsersInWorkspace({ page, pageSize }).pipe(
          unwrapClientReposne(),
          map((usersPage) =>
            actions.loadUsers.success({
              users: usersPage.items,
              page: usersPage.page,
              pageSize: usersPage.pageSize,
              totalCount: usersPage.totalCount,
              totalPages: usersPage.totalPages,
            })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadUsers.fail({ error }))
          )
        )
      )
    );
  });

  reloadUsersOnPaginationChange$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.setUsersPage, actions.setUsersPageSize),
      map(() => actions.loadUsers.init())
    );
  });

  loadUser$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadUser.init),
      switchMap(({ userId }) =>
        this.usersService.getUser(userId).pipe(
          map((user) => actions.loadUser.success({ user })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadUser.fail({ error }))
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
      ofType(actions.inviteUsersToWorkspace.init),
      switchMap(({ emailAddresses }) =>
        this.usersService.inviteUsersToWorkspace(emailAddresses).pipe(
          tap(() => this.snackbar.open('Invite(s) Sent')),
          mergeMap(() => [
            actions.inviteUsersToWorkspace.success({ emailAddresses }),
            actions.loadUsers.init(),
          ]),
          catchError((error) =>
            of(actions.inviteUsersToWorkspace.fail({ error }))
          )
        )
      )
    );
  });

  toggleUserPermission$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.toggleUserPermission.init),
      switchMap(({ userId, permission }) =>
        this.usersService.toggleUserPermission(userId, permission).pipe(
          tap(() => this.snackbar.open('Permission updated')),
          map(() =>
            actions.toggleUserPermission.success({ userId, permission })
          ),
          catchError((error) =>
            of(actions.toggleUserPermission.fail({ error }))
          )
        )
      )
    );
  });

  updateWorkspaceRole$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.updateWorkspaceRole.init),
      switchMap(({ userId, role }) =>
        this.usersService.updateWorkspaceRole(userId, role).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Workspace role updated')),
          map((result) =>
            actions.updateWorkspaceRole.success({
              userId,
              role: result.role,
              permissions: result.permissions,
            })
          ),
          catchError((error) => of(actions.updateWorkspaceRole.fail({ error })))
        )
      )
    );
  });

  resendInvite$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.resendInvite.init),
      switchMap(({ email }) =>
        this.usersService.resendInvite(email).pipe(
          tap(() => this.snackbar.open('Invite resent')),
          map(() => actions.resendInvite.success()),
          catchError((error) => of(actions.resendInvite.fail({ error })))
        )
      )
    );
  });

  removeUsers$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.removeUsersFromWorkspace.init),
      switchMap(({ emailAddresses }) =>
        this.confirmation.open(REMOVE_USERS_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return EMPTY;
            return this.usersService
              .removeUsersFromWorkspace(emailAddresses)
              .pipe(
                tap(() => this.snackbar.open('User(s) removed')),
                map(() =>
                  actions.removeUsersFromWorkspace.success({ emailAddresses })
                ),
                catchError((error) =>
                  of(actions.removeUsersFromWorkspace.fail({ error }))
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
