import { AppState } from '@core/core.state';
import { createSelector, createFeatureSelector } from '@ngrx/store';
import { BoardsState } from './boards.model';

export interface State extends AppState {
  boards: BoardsState;
}

const selectBoardsFeature = createFeatureSelector<State, BoardsState>('boards');

export const selectAllBoards = createSelector(
  selectBoardsFeature,
  (state: BoardsState) => state?.boards
);

export const selectBoardsLoading = createSelector(
  selectBoardsFeature,
  (state: BoardsState) => state.loading
);

export const selectBoardsLoaded = createSelector(
  selectBoardsFeature,
  (state: BoardsState) => state.loaded
);
