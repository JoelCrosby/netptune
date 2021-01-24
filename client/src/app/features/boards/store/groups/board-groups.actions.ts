import { HttpErrorResponse } from '@angular/common/http';
import { AddBoardGroupRequest } from '@core/models/add-board-group-request';
import { AppUser } from '@core/models/appuser';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { BoardGroupViewModel } from '@core/models/view-models/board-group-view-model';
import { BoardView, BoardViewGroup } from '@core/models/view-models/board-view';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[BoardGroups] Clear State');

// Load Board Groups

export const loadBoardGroups = createAction('[BoardGroups] Load Board Groups');

export const loadBoardGroupsSuccess = createAction(
  '[BoardGroups] Load Board Groups Success ',
  props<{
    boardGroups: BoardView;
    selectedIds: string[];
    onlyFlagged?: boolean;
    searchTerm?: string;
  }>()
);

export const loadBoardGroupsFail = createAction(
  '[BoardGroups] Load Board Groups Fail',
  props<{ error: HttpErrorResponse }>()
);

// Create Board Group

export const createBoardGroup = createAction(
  '[BoardGroups] Create Board Group',
  props<{ identifier: string; request: AddBoardGroupRequest }>()
);

export const createBoardGroupSuccess = createAction(
  '[BoardGroups] Create Board Group Success',
  props<{ boardGroup: BoardGroupViewModel }>()
);

export const createBoardGroupFail = createAction(
  '[BoardGroups] Create Board Group Fail',
  props<{ error: HttpErrorResponse }>()
);

// Select Board Group

export const selectBoardGroup = createAction(
  '[BoardGroups] Select Board Group',
  props<{ boardGroup: BoardGroupViewModel }>()
);

// Delete Board Group

export const deleteBoardGroup = createAction(
  '[BoardGroups] Delete Board Group',
  props<{ boardGroup: BoardViewGroup }>()
);

export const deleteBoardGroupSuccess = createAction(
  '[BoardGroups] Delete Board Group Success',
  props<{ boardGroupId: number }>()
);

export const deleteBoardGroupFail = createAction(
  '[BoardGroups] Delete Board Group Fail',
  props<{ error: HttpErrorResponse }>()
);

// Edit Board Group

export const editBoardGroup = createAction(
  '[BoardGroups] Edit Board Group',
  props<{ request: UpdateBoardGroupRequest }>()
);

export const editBoardGroupSuccess = createAction(
  '[BoardGroups] Edit Board Group Success',
  props<{ boardGroup: BoardGroupViewModel }>()
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

// Selected Users

export const toggleUserSelection = createAction(
  '[BoardGroups] Toggle Users Selection',
  props<{ user: AppUser }>()
);

// Selected Users

export const toggleOnlyFlagged = createAction(
  '[BoardGroups] Toggle Only Flagged'
);

// Set Search Term

export const setSearchTerm = createAction(
  '[BoardGroups] Set Search Term',
  props<{ term: string }>()
);

// Select Task

export const selectTask = createAction(
  '[BoardGroups] Select Task',
  props<{ id: number }>()
);

// Select Task Bulk

export const selectTaskBulk = createAction(
  '[BoardGroups] Select Task Bulk',
  props<{ id: number; groupId: number }>()
);

// Deselect Task

export const deSelectTask = createAction(
  '[BoardGroups] Deselect Task',
  props<{ id: number }>()
);

// Deselect Task Bulk

export const deSelectTaskBulk = createAction(
  '[BoardGroups] Deselect Task Bulk',
  props<{ id: number; groupId: number }>()
);

// Clear Task Selection

export const clearTaskSelection = createAction(
  '[BoardGroups] Clear Task Selection'
);

// Delete Selected Tasks

export const deleteSelectedTasks = createAction(
  '[BoardGroups] Delete Selected Tasks'
);

// Delete Task Multiple

export const deleteTaskMultiple = createAction(
  '[BoardGroups] Delete Task Multiple',
  props<{ ids: number[] }>()
);

export const deleteTasksMultipleSuccess = createAction(
  '[BoardGroups] Delete Task Multiple Success'
);

export const deleteTasksMultipleFail = createAction(
  '[BoardGroups] Delete Task Multiple Fail',
  props<{ error: HttpErrorResponse }>()
);

// Move Selected Tasks

export const moveSelectedTasks = createAction(
  '[BoardGroups] Move Selected Tasks',
  props<{ newGroupId: number }>()
);

export const moveSelectedTasksSuccess = createAction(
  '[BoardGroups] Move Selected Tasks Success'
);

export const moveSelectedTasksFail = createAction(
  '[BoardGroups] Move Selected Tasks Fail',
  props<{ error: HttpErrorResponse }>()
);

// Re-assign Tasks

export const reassignTasks = createAction(
  '[BoardGroups] Re-assigned Tasks',
  props<{ assigneeId: string }>()
);

export const reassignTasksSuccess = createAction(
  '[BoardGroups] Re-assigned Tasks Success'
);

export const reassignTasksFail = createAction(
  '[BoardGroups] Re-assigned Tasks Fail',
  props<{ error: HttpErrorResponse }>()
);
