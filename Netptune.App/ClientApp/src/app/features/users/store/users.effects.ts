import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, map, switchMap, switchAll, withLatestFrom } from 'rxjs/operators';
import {
  ActionLoadUsersSuccess,
  UsersActions,
  UsersActionTypes,
  ActionLoadUsersFail,
} from './users.actions';
import { UsersService } from './users.service';
import { SelectCurrentWorkspace } from '@app/core/state/core.selectors';
import { AppState } from '@app/core/core.state';
import { Store } from '@ngrx/store';

@Injectable()
export class UsersEffects {
  constructor(
    private actions$: Actions<UsersActions>,
    private usersService: UsersService,
    private store: Store<AppState>
  ) {}

  @Effect()
  loadUsers$ = this.actions$.pipe(
    ofType(UsersActionTypes.LoadUsers),
    withLatestFrom(this.store.select(SelectCurrentWorkspace)),
    switchMap(([action, workspace]) =>
      this.usersService.get(workspace).pipe(
        map(users => new ActionLoadUsersSuccess(users)),
        catchError(error => of(new ActionLoadUsersFail(error)))
      )
    )
  );
}
