import { createAction, props } from '@ngrx/store';
import { Board } from '@app/core/models/board';

export const clearState = createAction('[Boards] Clear State');

export const loadBoards = createAction('[Boards] Load Boards');

export const loadBoardsSuccess = createAction(
  '[Boards] Load Boards Success ',
  props<{ boards: Board[] }>()
);

export const loadBoardsFail = createAction(
  '[Boards] Load Boards Fail',
  props<{ error: any }>()
);

export const createBoard = createAction(
  '[Boards] Create Board',
  props<{ board: Board }>()
);

export const createBoardSuccess = createAction(
  '[Boards] Create Board Success',
  props<{ board: Board }>()
);

export const createBoardFail = createAction(
  '[Boards] Create Board Fail',
  props<{ error: any }>()
);

export const selectBoard = createAction(
  '[Boards] Select Board',
  props<{ board: Board }>()
);

export const deleteBoard = createAction(
  '[Boards] Delete Board',
  props<{ board: Board }>()
);

export const deleteBoardSuccess = createAction(
  '[Boards] Delete Board Success',
  props<{ board: Board }>()
);

export const deleteBoardFail = createAction(
  '[Boards] Delete Board Fail',
  props<{ error: any }>()
);
