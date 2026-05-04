import { selectActivitesFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { ActivityState } from './activity.model';

export const selectActivities = createSelector(
  selectActivitesFeature,
  (state: ActivityState) => state.activities
);

export const selectActivitiesLoading = createSelector(
  selectActivitesFeature,
  (state: ActivityState) => state.loading
);

export const selectActivitiesLoaded = createSelector(
  selectActivitesFeature,
  (state: ActivityState) => state.loaded
);

export const selectActivityNextCursor = createSelector(
  selectActivitesFeature,
  (state: ActivityState) => state.nextCursor
);

export const selectActivityPageSize = createSelector(
  selectActivitesFeature,
  (state: ActivityState) => state.pageSize
);

export const selectActivityCanLoadMore = createSelector(
  selectActivityNextCursor,
  (cursor) => !!cursor
);
