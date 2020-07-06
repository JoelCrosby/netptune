import { Injectable } from '@angular/core';
import { AppState } from '@core/core.state';
import { SelectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap, withLatestFrom } from 'rxjs/operators';
import * as actions from './users.actions';
import { UsersService } from './users.service';
import { selectWorkspace } from '@core/store/workspaces/workspaces.actions';

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

  constructor(
    private actions$: Actions<Action>,
    private usersService: UsersService,
    private store: Store<AppState>
  ) {}
}
