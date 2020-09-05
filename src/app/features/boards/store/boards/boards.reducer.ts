import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './boards.actions';
import { adapter, BoardsState, initialState } from './boards.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),

  // Load Boards

  on(actions.loadBoards, (state) => ({ ...state, loading: true })),
  on(actions.loadBoardsFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.loadBoardsSuccess, (state, { boards }) =>
    adapter.setAll(boards, { ...state, loading: false, loaded: true })
  ),

  // Create Board

  on(actions.createBoard, (state) => ({ ...state, loading: true })),
  on(actions.createBoardFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.createBoardSuccess, (state, { response }) =>
    response.isSuccess
      ? adapter.addOne(response.payload, {
          ...state,
          loadingCreate: false,
        })
      : state
  ),

  // Delete Board

  on(actions.deleteBoard, (state) => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteBoardFail, (state, { error }) => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteBoardSuccess, (state, { response }) =>
    response.isSuccess
      ? adapter.removeOne(response.payload.id, {
          ...state,
          deleteState: { loading: false },
        })
      : state
  )
);

export function boardsReducer(
  state: BoardsState | undefined,
  action: Action
): BoardsState {
  return reducer(state, action);
}
