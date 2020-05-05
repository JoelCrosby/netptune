import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { Injectable } from '@angular/core';
import { AppState } from '@core/core.state';
import { Actions, Effect, ofType, createEffect } from '@ngrx/effects';
import { Store, Action } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap, withLatestFrom } from 'rxjs/operators';
import * as actions from './profile.actions';
import { ProfileService } from './profile.service';

@Injectable()
export class ProfileEffects {
  loadProfile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadProfile),
      withLatestFrom(this.store.select(selectCurrentUser)),
      switchMap(([action, user]) =>
        this.profileService.get(user.userId).pipe(
          map(profile => actions.loadProfileSuccess({ profile })),
          catchError(error => of(actions.loadProfileFail({ error })))
        )
      )
    )
  );

  updateProfile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.updateProfile),
      switchMap(action =>
        this.profileService.put(action.profile).pipe(
          map(profile => actions.updateProfileSuccess({ profile })),
          catchError(error => of(actions.updateProfileFail({ error })))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private profileService: ProfileService,
    private store: Store<AppState>
  ) {}
}
