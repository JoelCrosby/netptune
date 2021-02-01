import { HttpErrorResponse } from '@angular/common/http';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { BoardsViewModel } from '@core/models/view-models/boards-view-model';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[Boards] Clear State');

// Load Boards

export const loadBoards = createAction('[Boards] Load Boards');

export const loadBoardsSuccess = createAction(
  '[Boards] Load Boards Success ',
  props<{ boards: BoardsViewModel[] }>()
);

export const loadBoardsFail = createAction(
  '[Boards] Load Boards Fail',
  props<{ error: HttpErrorResponse }>()
);

// Create Board

export const createBoard = createAction(
  '[Boards] Create Board',
  props<{ request: AddBoardRequest }>()
);

export const createBoardSuccess = createAction(
  '[Boards] Create Board Success',
  props<{ response: BoardViewModel }>()
);

export const createBoardFail = createAction(
  '[Boards] Create Board Fail',
  props<{ error: HttpErrorResponse }>()
);

// Delete Board

export const deleteBoard = createAction(
  '[Boards] Delete Board',
  props<{ boardId: number }>()
);

export const deleteBoardSuccess = createAction(
  '[Boards] Delete Board Success',
  props<{ boardId: number }>()
);

export const deleteBoardFail = createAction(
  '[Boards] Delete Board Fail',
  props<{ error: HttpErrorResponse }>()
);
