import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';

import { concatMap } from 'rxjs/operators';
import { EMPTY } from 'rxjs';
import { UsersActionTypes, UsersActions } from './users.actions';


@Injectable()
export class UsersEffects {


  @Effect()
  loadUserss$ = this.actions$.pipe(
    ofType(UsersActionTypes.LoadUserss),
    /** An EMPTY observable only emits completion. Replace with your own observable API request */
    concatMap(() => EMPTY)
  );


  constructor(private actions$: Actions<UsersActions>) {}

}
