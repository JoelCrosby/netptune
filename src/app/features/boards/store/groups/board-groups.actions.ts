import { BoardGroup } from '@core/models/board-group';
import { createAction, props } from '@ngrx/store';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddBoardGroupRequest } from '@core/models/add-board-group-request';
import { HttpErrorResponse } from '@angular/common/http';
import { BoardGroupsViewModel } from '@core/models/view-models/board-groups-view-model';
import { AddProjectTaskRequest } from '@app/core/models/project-task';
import { TaskViewModel } from '@app/core/models/view-models/project-task-dto';

export const clearState = createAction('[BoardGroups] Clear State');

// Load Board Groups

export const loadBoardGroups = createAction('[BoardGroups] Load Board Groups');

export const loadBoardGroupsSuccess = createAction(
  '[BoardGroups] Load Board Groups Success ',
  props<{ boardGroups: BoardGroupsViewModel }>()
);

export const loadBoardGroupsFail = createAction(
  '[BoardGroups] Load Board Groups Fail',
  props<{ error: HttpErrorResponse }>()
);

// Create Board Group

export const createBoardGroup = createAction(
  '[BoardGroups] Create Board Group',
  props<{ request: AddBoardGroupRequest }>()
);

export const createBoardGroupSuccess = createAction(
  '[BoardGroups] Create Board Group Success',
  props<{ boardGroup: BoardGroup }>()
);

export const createBoardGroupFail = createAction(
  '[BoardGroups] Create Board Group Fail',
  props<{ error: HttpErrorResponse }>()
);

// Select Board Group

export const selectBoardGroup = createAction(
  '[BoardGroups] Select Board Group',
  props<{ boardGroup: BoardGroup }>()
);

// Delete Board Group

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
  props<{ error: HttpErrorResponse }>()
);

// Edit Board Group

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
  props<{ error: HttpErrorResponse }>()
);

// Move Task In BoardGroup

export const moveTaskInBoardGroup = createAction(
  '[BoardGroups] Move Task In BoardGroup',
  props<{ request: MoveTaskInGroupRequest }>()
);

export const moveTaskInBoardGroupSuccess = createAction(
  '[BoardGroups] Move Task In BoardGroup Success'
);

export const moveTaskInBoardGroupFail = createAction(
  '[BoardGroups] Move Task In BoardGroup Fail',
  props<{ error: HttpErrorResponse }>()
);

export const setIsDragging = createAction(
  '[BoardGroups] Set Is Dragging',
  props<{ isDragging: boolean }>()
);

export const setInlineActive = createAction(
  '[BoardGroups] Set Is Inline Active',
  props<{ groupId: number }>()
);

export const clearInlineActive = createAction(
  '[BoardGroups] Clear Inline Active'
);

// Create Task

export const createProjectTask = createAction(
  '[BoardGroups] Create Project Task',
  props<{ task: AddProjectTaskRequest }>()
);

export const createProjectTasksSuccess = createAction(
  '[BoardGroups] Create Project Task Success',
  props<{ task: TaskViewModel }>()
);

export const createProjectTasksFail = createAction(
  '[BoardGroups] Create Project Task Fail',
  props<{ error: HttpErrorResponse }>()
);
