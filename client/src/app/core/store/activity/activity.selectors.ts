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
