import { EntityType } from '@core/models/entity-type';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[Activity] Clear State');

// Load Activity

export const loadActivity = createAction(
  '[Activity] Load Activity',
  props<{ entityType: EntityType; entityId: number }>()
);

export const loadMoreActivity = createAction(
  '[Activity] Load More Activity',
  props<{ entityType: EntityType; entityId: number }>()
);

export const loadActivitySuccess = createAction(
  '[Activity] Load Activity Success',
  props<{
    activities: ActivityViewModel[];
    nextCursor?: string;
    pageSize: number;
  }>()
);

export const loadMoreActivitySuccess = createAction(
  '[Activity] Load More Activity Success',
  props<{
    activities: ActivityViewModel[];
    nextCursor?: string;
    pageSize: number;
  }>()
);

export const loadActivityFail = createAction(
  '[Activity] Load Activity Fail',
  props<{ error: HttpErrorResponse }>()
);
