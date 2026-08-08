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

export const selectSprintsLoaded = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.loaded
);

export const selectSprintsFilter = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.filter
);

export const selectCurrentSprints = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.currentSprints
);

export const selectCurrentSprintsLoaded = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.currentSprintsLoaded
);

export const selectSprintDetail = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.detail
);

export const selectSprintDetailLoading = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.detailLoading
);

export const selectSprintDetailError = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.detailError
);

export const selectSprintCreateLoading = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.createState.loading
);

export const selectSprintUpdateLoading = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.updateState.loading
);
