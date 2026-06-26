import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './boards.actions';
import { BoardsState, initialState } from './boards.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): BoardsState => initialState),

  // Load Boards

  on(actions.loadBoards.init, (state): BoardsState => ({ ...state, loading: true })),
  on(
    actions.loadBoards.fail,
    (state, { error }): BoardsState => ({
      ...state,
      loadingError: error,
      loading: false,
    })
  ),
  on(
    actions.loadBoards.success,
    (state, { boards }): BoardsState => ({
      ...state,
      boards,
      loading: false,
    })
  ),

  // Create Board

  on(
    actions.createBoard.init,
    (state): BoardsState => ({ ...state, loadingCreate: true })
  ),
  on(
    actions.createBoard.fail,
    (state, { error }): BoardsState => ({
      ...state,
      loadingError: error,
    })
  ),
  on(
    actions.createBoard.success,
    (state): BoardsState => ({
      ...state,
      loadingCreate: false,
    })
  ),

  // Delete Board

  on(
    actions.deleteBoard.init,
    (state): BoardsState => ({
      ...state,
      deleteState: { loading: true },
    })
  ),
  on(
    actions.deleteBoard.fail,
    (state, { error }): BoardsState => ({
      ...state,
      deleteState: { loading: false, error },
    })
  ),
  on(
    actions.deleteBoard.success,
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
