import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { currentUser } from '@core/auth/store/auth.actions';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import {
  catchError,
  debounceTime,
  filter,
  map,
  switchMap,
  tap,
} from 'rxjs/operators';
import * as actions from './profile.actions';
import { ProfileService } from './profile.service';

@Injectable()
export class ProfileEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private profileService = inject(ProfileService);
  private store = inject(Store);
  private snackbar = inject(MatSnackBar);

  loadProfile$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadProfile),
      concatLatestFrom(() => this.store.select(selectCurrentUser)),
      switchMap(([_, user]) => {
        if (!user) return of({ type: 'noop' });

        return this.profileService.get(user.userId).pipe(
          map((profile) => actions.loadProfileSuccess({ profile })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadProfileFail({ error }))
          )
        );
      })
    );
  });

  updateProfile$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.updateProfile),
      switchMap(({ profile }) => {
        if (!profile) return of({ type: 'noop' });

        return this.profileService.put(profile).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Profile Updated')),
          map((response) =>
            actions.updateProfileSuccess({ profile: response })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateProfileFail({ error }))
          )
        );
      })
    );
  });

  updateProfileSuccess$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.updateProfileSuccess, actions.uploadProfilePictureSuccess),
      debounceTime(280),
      map(() => currentUser())
    );
  });

  changePassword$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.changePassword),
      switchMap((action) =>
        this.profileService.changePassword(action.request).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Password Changed')),
          map(() => actions.changePasswordSuccess()),
          catchError((error) => of(actions.changePasswordFail({ error })))
        )
      )
    );
  });

  updateProfileWithImage$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.updateProfile),
      map((action) => {
        if (!action.image) {
          return { type: 'noop' };
        }

        return actions.uploadProfilePicture({ data: action.image });
      })
    );
  });

  uploadProfilePicture$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.uploadProfilePicture),
      filter((action) => !!action.data),
      switchMap((action) =>
        this.profileService.uploadProfilePicture(action.data).pipe(
          unwrapClientReposne(),
          map((response) => actions.uploadProfilePictureSuccess({ response })),
          catchError((error: HttpErrorResponse) =>
            of(actions.uploadProfilePictureFail({ error }))
          )
        )
      )
    );
  });
}
