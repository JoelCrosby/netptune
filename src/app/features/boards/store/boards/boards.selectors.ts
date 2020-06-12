import { AppState } from '@core/core.state';
import { createSelector, createFeatureSelector } from '@ngrx/store';
import { adapter, BoardsState } from './boards.model';
import { Board } from '@app/core/models/board';

export interface State extends AppState {
  boards: BoardsState;
}

const selectBoardsFeature = createFeatureSelector<State, BoardsState>('boards');

const { selectEntities, selectAll } = adapter.getSelectors();

export const selectAllBoards = createSelector(selectBoardsFeature, selectAll);

export const selectBoardsEntities = createSelector(
  selectBoardsFeature,
  selectEntities
);

export const selectBoardsLoading = createSelector(
  selectBoardsFeature,
  (state: BoardsState) => state.loading
);

export const selectBoardsLoaded = createSelector(
  selectBoardsFeature,
  (state: BoardsState) => state.loaded
);

export const selectCurrentBoard = createSelector(
  selectBoardsFeature,
  (state: BoardsState) => state.currentBoard
);

export const selectCurrentBoardId = createSelector(
  selectCurrentBoard,
  (state: Board) => state.id
);
