import { Injectable } from '@angular/core';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';
import { SelectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
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

@Injectable()
export class UsersEffects {
  loadUsers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadUsers),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([action, workspace]) =>
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

  ionviteUsers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.inviteUsersToWorkspace),
      withLatestFrom(this.store.select(SelectCurrentWorkspace)),
      switchMap(([{ emailAddresses }, workspace]) =>
        this.usersService
          .inviteUsersToWorkspace(emailAddresses, workspace.slug)
          .pipe(
            tap(() => this.snackbar.open('Invite(s) Sent')),
            map((users) => actions.inviteUsersToWorkspaceSuccess({ users })),
            catchError((error) =>
              of(actions.inviteUsersToWorkspaceFail({ error }))
            )
          )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private usersService: UsersService,
    private snackbar: MatSnackBar,
    private store: Store
  ) {}
}
