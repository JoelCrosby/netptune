import { BoardGroup } from '@app/core/models/board-group';
import { createAction, props } from '@ngrx/store';
import { MoveTaskInGroupRequest } from '@app/core/models/move-task-in-group-request';

export const loadBoardGroups = createAction('[BoardGroups] Load Board Groups');

export const loadBoardGroupsSuccess = createAction(
  '[BoardGroups] Load Board Groups Success ',
  props<{ boardGroups: BoardGroup[] }>()
);

export const loadBoardGroupsFail = createAction(
  '[BoardGroups] Load Board Groups Fail',
  props<{ error: any }>()
);

export const createBoardGroup = createAction(
  '[BoardGroups] Create Board Group',
  props<{ boardGroup: BoardGroup }>()
);

export const createBoardGroupSuccess = createAction(
  '[BoardGroups] Create Board Group Success',
  props<{ boardGroup: BoardGroup }>()
);

export const createBoardGroupFail = createAction(
  '[BoardGroups] Create Board Group Fail',
  props<{ error: any }>()
);

export const selectBoardGroup = createAction(
  '[BoardGroups] Select Board Group',
  props<{ boardGroup: BoardGroup }>()
);

export const deleteBoardGroup = createAction(
  '[BoardGroups] Delete Board Group',
  props<{ boardGroup: BoardGroup }>()
);

export const deleteBoardGroupSuccess = createAction(
  '[BoardGroups] Delete Board Group Success',
  props<{ boardGroup: BoardGroup }>()
);

export const deleteBoardGroupFail = createAction(
  '[BoardGroups] Delete Board Group Fail',
  props<{ error: any }>()
);

export const editBoardGroup = createAction(
  '[BoardGroups] Edit Board Group',
  props<{ boardGroup: BoardGroup }>()
);

export const editBoardGroupSuccess = createAction(
  '[BoardGroups] Edit Board Group Success',
  props<{ boardGroup: BoardGroup }>()
);

export const editBoardGroupFail = createAction(
  '[BoardGroups] Edit Board Group Fail',
  props<{ error: any }>()
);

export const moveTaskInBoardGroup = createAction(
  '[BoardGroups] Move Task In BoardGroup',
  props<{ request: MoveTaskInGroupRequest }>()
);

export const moveTaskInBoardGroupSuccess = createAction(
  '[BoardGroups] Move Task In BoardGroup Success'
);

export const moveTaskInBoardGroupFail = createAction(
  '[BoardGroups] Move Task In BoardGroup Fail',
  props<{ error: any }>()
);
