import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatLegacySnackBar as MatSnackBar } from '@angular/material/legacy-snack-bar';
import { currentUser } from '@core/auth/store/auth.actions';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import {
  catchError,
  debounceTime,
  filter,
  map,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs/operators';
import * as actions from './profile.actions';
import { ProfileService } from './profile.service';

@Injectable()
export class ProfileEffects {
  loadProfile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadProfile),
      withLatestFrom(this.store.select(selectCurrentUser)),
      switchMap(([_, user]) => {
        if (!user) return of({ type: 'noop' });

        return this.profileService.get(user.userId).pipe(
          map((profile) => actions.loadProfileSuccess({ profile })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadProfileFail({ error }))
          )
        );
      })
    )
  );

  updateProfile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.updateProfile),
      switchMap(({ profile, data }) => {
        if (!profile || !data) return of({ type: 'noop' });

        return this.profileService.put(profile).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Profile Updated')),
          tap(() =>
            this.store.dispatch(actions.uploadProfilePicture({ data }))
          ),
          map((response) =>
            actions.updateProfileSuccess({ profile: response })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateProfileFail({ error }))
          )
        );
      })
    )
  );

  updateProfileSuccess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.updateProfileSuccess, actions.uploadProfilePictureSuccess),
      debounceTime(280),
      map(() => currentUser())
    )
  );

  changePassword$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.changePassword),
      switchMap((action) =>
        this.profileService.changePassword(action.request).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Password Changed')),
          map(() => actions.changePasswordSuccess()),
          catchError(() => of(actions.changePasswordFail()))
        )
      )
    )
  );

  uploadProfilePicture$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.uploadProfilePicture),
      filter((action) => !!action.data),
      switchMap((action) =>
        this.profileService.uloadProfilePicture(action.data).pipe(
          unwrapClientReposne(),
          map((response) => actions.uploadProfilePictureSuccess({ response })),
          catchError((error: HttpErrorResponse) =>
            of(actions.uploadProfilePictureFail({ error }))
          )
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private profileService: ProfileService,
    private store: Store,
    private snackbar: MatSnackBar
  ) {}
}
