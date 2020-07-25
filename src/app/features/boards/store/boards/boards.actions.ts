import { createAction, props } from '@ngrx/store';
import { Board } from '@app/core/models/board';
import { BoardViewModel } from '@app/core/models/view-models/board-view-model';
import { HttpErrorResponse } from '@angular/common/http';

export const clearState = createAction('[Boards] Clear State');

export const loadBoards = createAction('[Boards] Load Boards');

export const loadBoardsSuccess = createAction(
  '[Boards] Load Boards Success ',
  props<{ boards: BoardViewModel[] }>()
);

export const loadBoardsFail = createAction(
  '[Boards] Load Boards Fail',
  props<{ error: HttpErrorResponse }>()
);

export const createBoard = createAction(
  '[Boards] Create Board',
  props<{ board: Board }>()
);

export const createBoardSuccess = createAction(
  '[Boards] Create Board Success',
  props<{ board: BoardViewModel }>()
);

export const createBoardFail = createAction(
  '[Boards] Create Board Fail',
  props<{ error: HttpErrorResponse }>()
);

export const deleteBoard = createAction(
  '[Boards] Delete Board',
  props<{ board: BoardViewModel }>()
);

export const deleteBoardSuccess = createAction(
  '[Boards] Delete Board Success',
  props<{ board: BoardViewModel }>()
);

export const deleteBoardFail = createAction(
  '[Boards] Delete Board Fail',
  props<{ error: HttpErrorResponse }>()
);
