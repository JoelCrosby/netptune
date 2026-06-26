import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { currentUser } from '@app/core/store/auth/auth.actions';
import { selectCurrentUser } from '@app/core/store/auth/auth.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { Action, Store } from '@ngrx/store';
import { EMPTY, of } from 'rxjs';
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
  private snackbar = inject(SnackbarService);

  loadProfile$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadProfile.init),
      concatLatestFrom(() => this.store.select(selectCurrentUser)),
      switchMap(([_, user]) => {
        if (!user) return EMPTY;

        return this.profileService.get(user.userId).pipe(
          map((profile) => actions.loadProfile.success({ profile })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadProfile.fail({ error }))
          )
        );
      })
    );
  });

  updateProfile$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.updateProfile.init),
      switchMap(({ profile }) => {
        if (!profile) return EMPTY;

        return this.profileService.put(profile).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Profile Updated')),
          map((response) =>
            actions.updateProfile.success({ profile: response })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.updateProfile.fail({ error }))
          )
        );
      })
    );
  });

  updateProfileSuccess$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(
        actions.updateProfile.success,
        actions.uploadProfilePicture.success
      ),
      debounceTime(280),
      map(() => currentUser.init())
    );
  });

  changePassword$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.changePassword.init),
      switchMap((action) =>
        this.profileService.changePassword(action.request).pipe(
          unwrapClientReposne(),
          tap(() => this.snackbar.open('Password Changed')),
          map(() => actions.changePassword.success()),
          catchError((error) => of(actions.changePassword.fail({ error })))
        )
      )
    );
  });

  updateProfileWithImage$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.updateProfile.init),
      filter((action) => !!action.image),
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
      map((action) => actions.uploadProfilePicture.init({ data: action.image! }))
    );
  });

  uploadProfilePicture$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.uploadProfilePicture.init),
      filter((action) => !!action.data),
      switchMap((action) =>
        this.profileService.uploadProfilePicture(action.data).pipe(
          unwrapClientReposne(),
          map((response) => actions.uploadProfilePicture.success({ response })),
          catchError((error: HttpErrorResponse) =>
            of(actions.uploadProfilePicture.fail({ error }))
          )
        )
      )
    );
  });

  loadLoginProviders$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadProfile.init),
      switchMap(() =>
        this.profileService.getLoginProviders().pipe(
          map((providers) => actions.loadLoginProviders.success({ providers })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadLoginProviders.fail({ error }))
          )
        )
      )
    );
  });
}
