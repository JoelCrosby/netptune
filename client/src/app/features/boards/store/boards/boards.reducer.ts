import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './boards.actions';
import { BoardsState, initialState } from './boards.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): BoardsState => initialState),

  // Load Boards

  on(actions.loadBoards, (state): BoardsState => ({ ...state, loading: true })),
  on(
    actions.loadBoardsFail,
    (state, { error }): BoardsState => ({
      ...state,
      loadingError: error,
      loading: false,
    })
  ),
  on(
    actions.loadBoardsSuccess,
    (state, { boards }): BoardsState => ({
      ...state,
      boards,
      loading: false,
    })
  ),

  // Create Board

  on(
    actions.createBoard,
    (state): BoardsState => ({ ...state, loadingCreate: true })
  ),
  on(
    actions.createBoardFail,
    (state, { error }): BoardsState => ({
      ...state,
      loadingError: error,
    })
  ),
  on(
    actions.createBoardSuccess,
    (state): BoardsState => ({
      ...state,
      loadingCreate: false,
    })
  ),

  // Delete Board

  on(
    actions.deleteBoard,
    (state): BoardsState => ({
      ...state,
      deleteState: { loading: true },
    })
  ),
  on(
    actions.deleteBoardFail,
    (state, { error }): BoardsState => ({
      ...state,
      deleteState: { loading: false, error },
    })
  ),
  on(
    actions.deleteBoardSuccess,
    (state): BoardsState => ({
      ...state,
      deleteState: { loading: false },
    })
  )
);

export const boardsReducer = (
  state: BoardsState | undefined,
  action: Action
): BoardsState => reducer(state, action);
