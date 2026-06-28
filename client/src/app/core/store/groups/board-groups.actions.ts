import { AddBoardGroupRequest } from '@core/models/add-board-group-request';
import { AppUser } from '@core/models/appuser';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { BoardGroupViewModel } from '@core/models/view-models/board-group-view-model';
import { BoardView, BoardViewGroup } from '@core/models/view-models/board-view';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { FileResponse } from '@core/types/file-response';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';
import { BorderFilterParams } from './board-groups.model';

export const clearState = createAction('[BoardGroups] Clear State');

// Load Board Groups

export const loadBoardGroups = createAsyncAction(
  '[BoardGroups] Load Board Groups',
  {
    success: props<{
      boardGroups: BoardView;
      selectedIds: string[];
      selectedStatusIds?: number[];
      searchTerm?: string | null;
      sprintId?: number;
    }>(),
  }
);

// Create Board Group

export const createBoardGroup = createAsyncAction(
  '[BoardGroups] Create Board Group',
  {
    init: props<{ identifier: string; request: AddBoardGroupRequest }>(),
    success: props<{ boardGroup: BoardGroupViewModel }>(),
  }
);

// Select Board Group

export const selectBoardGroup = createAction(
  '[BoardGroups] Select Board Group',
  props<{ boardGroup: BoardGroupViewModel }>()
);

// Delete Board Group

export const deleteBoardGroup = createAsyncAction(
  '[BoardGroups] Delete Board Group',
  {
    init: props<{ boardGroup: BoardViewGroup }>(),
    success: props<{ boardGroupId: number }>(),
  }
);

// Edit Board Group

export const editBoardGroup = createAsyncAction(
  '[BoardGroups] Edit Board Group',
  {
    init: props<{ request: UpdateBoardGroupRequest }>(),
    success: props<{ boardGroup: BoardGroupViewModel }>(),
  }
);

// Move Task In BoardGroup

export const moveTaskInBoardGroup = createAsyncAction(
  '[BoardGroups] Move Task In BoardGroup',
  {
    init: props<{ request: MoveTaskInGroupRequest }>(),
  }
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

export const createProjectTask = createAsyncAction(
  '[BoardGroups] Create Project Task',
  {
    init: props<{ task: AddProjectTaskRequest }>(),
    success: props<{ task: TaskViewModel }>(),
  }
);

// Selected Users

export const toggleUserSelection = createAction(
  '[BoardGroups] Toggle Users Selection',
  props<{ user: AppUser }>()
);

// Set Online Users

export const setOnlineUsers = createAction(
  '[BoardGroups] Set Online Users',
  props<{ userIds: string[] }>()
);

// Toggle Status Selection

export const toggleStatusSelection = createAction(
  '[BoardGroups] Toggle Status Selection',
  props<{ status: number }>()
);

// Set Search Term

export const setSearchTerm = createAction(
  '[BoardGroups] Set Search Term',
  props<{ term?: string | null }>()
);

// Set Sprint Filter

export const setSprintFilter = createAction(
  '[BoardGroups] Set Sprint Filter',
  props<{ sprintId?: number }>()
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

export const deleteTaskMultiple = createAsyncAction(
  '[BoardGroups] Delete Task Multiple',
  {
    init: props<{ ids: number[] }>(),
  }
);

// Move Selected Tasks

export const moveSelectedTasks = createAsyncAction(
  '[BoardGroups] Move Selected Tasks',
  {
    init: props<{ newGroupId: number }>(),
  }
);

// Re-assign Tasks

export const reassignTasks = createAsyncAction(
  '[BoardGroups] Re-assigned Tasks',
  {
    init: props<{ assigneeId: string }>(),
  }
);

// Inline Task Content

export const setInlineTaskContent = createAction(
  '[BoardGroups] Set Inline Task Content',
  props<{ content: string | undefined | null }>()
);

// Set Inline Dirty

export const setIsInlineDirty = createAction(
  '[BoardGroups] Set Inline Dirty',
  props<{ isDirty: boolean }>()
);

// Export Board Tasks

export const exportBoardTasks = createAsyncAction(
  '[BoardGroups] Export Board Tasks',
  {
    success: props<{ reponse: FileResponse }>(),
  }
);

// Update Board Filter

export const updateBoardFilter = createAction(
  '[BoardGroups] Update Board Filter',
  props<{ params: BorderFilterParams }>()
);
