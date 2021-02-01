import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './boards.actions';
import { BoardsState, initialState } from './boards.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),

  // Load Boards

  on(actions.loadBoards, (state) => ({ ...state, loading: true })),
  on(actions.loadBoardsFail, (state, { error }) => ({
    ...state,
    loadingError: error,
    loading: false,
  })),
  on(actions.loadBoardsSuccess, (state, { boards }) => ({
    ...state,
    boards,
    loading: false,
  })),

  // Create Board

  on(actions.createBoard, (state) => ({ ...state, loadingCreate: true })),
  on(actions.createBoardFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.createBoardSuccess, (state) => ({
    ...state,
    loadingCreate: false,
  })),

  // Delete Board

  on(actions.deleteBoard, (state) => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteBoardFail, (state, { error }) => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteBoardSuccess, (state) => ({
    ...state,
    deleteState: { loading: false },
  }))
);

export const boardsReducer = (
  state: BoardsState | undefined,
  action: Action
): BoardsState => reducer(state, action);
