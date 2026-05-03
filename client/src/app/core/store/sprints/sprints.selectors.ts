import { selectSprintsFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { adapter, SprintsState } from './sprints.model';

const { selectAll, selectEntities } = adapter.getSelectors();

export const selectAllSprints = createSelector(selectSprintsFeature, selectAll);

export const selectSprintEntities = createSelector(
  selectSprintsFeature,
  selectEntities
);

export const selectSprintsLoading = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.loading && !state.loaded
);

export const selectSprintDetail = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.detail
);

export const selectSprintDetailLoading = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.detailLoading
);

export const selectAvailableSprintTasks = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.availableTasks
);

export const selectAvailableSprintTasksLoading = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.availableTasksLoading
);

export const selectSprintCreateLoading = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.createState.loading
);

export const selectSprintUpdateLoading = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.updateState.loading
);
