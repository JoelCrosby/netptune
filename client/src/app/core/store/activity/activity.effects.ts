import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import { catchError, debounceTime, map, switchMap } from 'rxjs/operators';
import * as actions from './activity.actions';
import { ActivityService } from './activity.service';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable()
export class ActivityEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private activityService = inject(ActivityService);

  loadActivities$ = createEffect(
    ({ debounce = 0, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.loadActivity),
        debounceTime(debounce, scheduler),
        switchMap(({ entityType, entityId }) =>
          this.activityService.get(entityType, entityId).pipe(
            unwrapClientReposne(),
            map((activities) => actions.loadActivitySuccess({ activities })),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadActivityFail({ error }))
            )
          )
        )
      )
  );
}
