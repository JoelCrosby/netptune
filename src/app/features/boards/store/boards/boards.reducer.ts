import { Action, createReducer, on } from '@ngrx/store';
import { adapter, initialState, BoardsState } from './boards.model';
import * as actions from './boards.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),
  on(actions.loadBoards, (state) => ({ ...state, loading: true })),
  on(actions.loadBoardsFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.loadBoardsSuccess, (state, { boards }) =>
    adapter.setAll(boards, { ...state, loading: false, loaded: true })
  ),
  on(actions.createBoard, (state) => ({ ...state, loading: true })),
  on(actions.createBoardFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.createBoardSuccess, (state, { board }) =>
    adapter.addOne(board, {
      ...state,
      loadingCreate: false,
    })
  ),
  on(actions.deleteBoard, (state) => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteBoardFail, (state, { error }) => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteBoardSuccess, (state, { board }) =>
    adapter.removeOne(board.id, {
      ...state,
      deleteState: { loading: false },
    })
  )
);

export function boardsReducer(state: BoardsState | undefined, action: Action) {
  return reducer(state, action);
}
