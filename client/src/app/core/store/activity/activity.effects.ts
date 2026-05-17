import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { Action, Store } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import {
  catchError,
  debounceTime,
  filter,
  map,
  switchMap,
} from 'rxjs/operators';
import * as actions from './activity.actions';
import { ActivityService } from './activity.service';
import { HttpErrorResponse } from '@angular/common/http';
import {
  selectActivityNextCursor,
  selectActivityPageSize,
} from './activity.selectors';

@Injectable()
export class ActivityEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private activityService = inject(ActivityService);
  private store = inject(Store);

  loadActivities$ = createEffect(
    ({ debounce = 0, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.loadActivity),
        debounceTime(debounce, scheduler),
        switchMap(({ entityType, entityId }) =>
          this.activityService.get(entityType, entityId).pipe(
            map((page) =>
              actions.loadActivitySuccess({
                activities: page.items,
                nextCursor: page.nextCursor,
                pageSize: page.pageLimit,
              })
            ),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadActivityFail({ error }))
            )
          )
        )
      );
    }
  );

  loadMoreActivities$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadMoreActivity),
      concatLatestFrom(() => [
        this.store.select(selectActivityNextCursor),
        this.store.select(selectActivityPageSize),
      ]),
      filter(([, cursor]) => !!cursor),
      switchMap(([{ entityType, entityId }, cursor, pageSize]) =>
        this.activityService
          .get(entityType, entityId, { cursor, take: pageSize })
          .pipe(
            map((page) =>
              actions.loadMoreActivitySuccess({
                activities: page.items,
                nextCursor: page.nextCursor,
                pageSize: page.pageLimit,
              })
            ),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadActivityFail({ error }))
            )
          )
      )
    );
  });
}
