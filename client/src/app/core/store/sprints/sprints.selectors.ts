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

export const selectCurrentSprints = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.currentSprints
);

export const selectCurrentSprint = createSelector(
  selectCurrentSprints,
  (sprints) => sprints[0]
);

export const selectCurrentSprintsLoading = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.currentSprintsLoading
);

export const selectCurrentSprintsLoaded = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.currentSprintsLoaded
);

export const selectSelectedSprintFilterId = createSelector(
  selectSprintsFeature,
  (state: SprintsState) => state.selectedSprintFilterId
);

export const selectSelectedSprintFilter = createSelector(
  selectSelectedSprintFilterId,
  selectCurrentSprints,
  selectSprintEntities,
  (sprintId, currentSprints, sprintEntities) => {
    if (!sprintId) return undefined;

    return (
      currentSprints.find((sprint) => sprint.id === sprintId) ??
      sprintEntities[sprintId]
    );
  }
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
