import { EntityType } from '@core/models/entity-type';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[Activity] Clear State');

// Load Activity

export const loadActivity = createAsyncAction('[Activity] Load Activity', {
  init: props<{ entityType: EntityType; entityId: number }>(),
  success: props<{
    activities: ActivityViewModel[];
    nextCursor?: string;
    pageSize: number;
  }>(),
});

// Load More Activity

export const loadMoreActivity = createAsyncAction(
  '[Activity] Load More Activity',
  {
    init: props<{ entityType: EntityType; entityId: number }>(),
    success: props<{
      activities: ActivityViewModel[];
      nextCursor?: string;
      pageSize: number;
    }>(),
  }
);
