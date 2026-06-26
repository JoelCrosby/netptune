import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { UpdateBoardRequest } from '@core/models/requests/update-board-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { BoardsViewModel } from '@core/models/view-models/boards-view-model';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[Boards] Clear State');

// Load Boards

export const loadBoards = createAsyncAction('[Boards] Load Boards', {
  success: props<{ boards: BoardsViewModel[] }>(),
});

// Create Board

export const createBoard = createAsyncAction('[Boards] Create Board', {
  init: props<{ request: AddBoardRequest }>(),
  success: props<{ response: BoardViewModel }>(),
});

// Delete Board

export const deleteBoard = createAsyncAction('[Boards] Delete Board', {
  init: props<{ boardId: number }>(),
  success: props<{ boardId: number }>(),
});

// Update Board

export const updateBoard = createAsyncAction('[Boards] Update Board', {
  init: props<{ request: UpdateBoardRequest }>(),
  success: props<{ response: BoardViewModel }>(),
});
