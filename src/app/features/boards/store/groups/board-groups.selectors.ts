import { AppState } from '@core/core.state';
import { createSelector, createFeatureSelector } from '@ngrx/store';
import { adapter, BoardGroupsState } from './board-groups.model';

export interface State extends AppState {
  boardgroups: BoardGroupsState;
}

const selectBoardGroupsFeature = createFeatureSelector<State, BoardGroupsState>(
  'boardgroups'
);

const { selectEntities, selectAll } = adapter.getSelectors();

export const selectAllBoardGroups = createSelector(
  selectBoardGroupsFeature,
  selectAll
);

export const selectBoardGroupEntities = createSelector(
  selectBoardGroupsFeature,
  selectEntities
);

export const selectBoardGroupsLoading = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.loading
);

export const selectBoardGroupsLoaded = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.loaded
);

export const selectCurrentBoardGroup = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.currentBoardGroup
);

export const selectIsDragging = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.isDragging
);

export const selectIsInlineActive = createSelector(
  selectBoardGroupsFeature,
  (state: BoardGroupsState) => state.isInlineActive
);
