import { createFeatureSelector, createSelector } from '@ngrx/store';
import { BoardsState } from './boards.model';

const selectBoardsFeature = createFeatureSelector<BoardsState>('boards');

export const selectAllBoards = createSelector(
  selectBoardsFeature,
  (state: BoardsState) => state?.boards || []
);

export const selectBoardsLoading = createSelector(
  selectBoardsFeature,
  (state: BoardsState) => state.loading
);

export const selectBoardsLoaded = createSelector(
  selectBoardsFeature,
  (state: BoardsState) => state.loaded
);
