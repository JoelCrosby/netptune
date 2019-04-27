import { selectCurrentUser } from '@app/core/auth/store/auth.selectors';
import { Injectable } from '@angular/core';
import { AppState } from '@app/core/core.state';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap, withLatestFrom } from 'rxjs/operators';
import {
  ActionLoadProfileFail,
  ActionLoadProfileSuccess,
  ActionUpdateProfileFail,
  ActionUpdateProfileSuccess,
  ProfileActions,
  ProfileActionTypes,
} from './profile.actions';
import { ProfileService } from './profile.service';

@Injectable()
export class ProfileEffects {
  constructor(
    private actions$: Actions<ProfileActions>,
    private profileService: ProfileService,
    private store: Store<AppState>
  ) {}

  @Effect()
  loadProfile$ = this.actions$.pipe(
    ofType(ProfileActionTypes.LoadProfile),
    withLatestFrom(this.store.select(selectCurrentUser)),
    switchMap(([action, user]) =>
      this.profileService.get(user.userId).pipe(
        map(profile => new ActionLoadProfileSuccess(profile)),
        catchError(error => of(new ActionLoadProfileFail(error)))
      )
    )
  );

  @Effect()
  updateProfile$ = this.actions$.pipe(
    ofType(ProfileActionTypes.UpdateProfile),
    switchMap(action =>
      this.profileService.post(action.payload).pipe(
        map(profile => new ActionUpdateProfileSuccess(profile)),
        catchError(error => of(new ActionUpdateProfileFail(error)))
      )
    )
  );
}
