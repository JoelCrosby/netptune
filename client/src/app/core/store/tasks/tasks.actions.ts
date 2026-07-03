import { Params } from '@angular/router';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { BulkUpdateTasksRequest } from '@core/models/requests/bulk-update-tasks-request';
import { UpdateProjectTaskRequest } from '@core/models/requests/update-project-task-request';
import { Tag } from '@core/models/tag';
import { BoardViewTask } from '@core/models/view-models/board-view';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { FileResponse } from '@core/types/file-response';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[ProjectTasks] Clear State');

// Load Tasks

export const loadProjectTasks = createAsyncAction(
  '[ProjectTasks] Load ProjectTasks',
  {
    success: props<{
      tasks: TaskViewModel[];
      page: number;
      pageSize: number;
      totalCount: number;
      totalPages: number;
    }>(),
  }
);

export const setProjectTasksPageSize = createAction(
  '[ProjectTasks] Set ProjectTasks Page Size',
  props<{ pageSize: number }>()
);

export const setProjectTasksPage = createAction(
  '[ProjectTasks] Set ProjectTasks Page',
  props<{ page: number }>()
);

export const hydrateProjectTaskFiltersFromRoute = createAction(
  '[ProjectTasks] Hydrate ProjectTask Filters From Route',
  props<{
    term?: string | null;
    assigneeIds: string[];
    statuses: number[];
    tags: string[];
    sprintId?: number;
  }>()
);

export const updateProjectTasksFilter = createAction(
  '[ProjectTasks] Update ProjectTasks Filter',
  props<{ params: Params }>()
);

// Create Task

export const createProjectTask = createAsyncAction(
  '[ProjectTasks] Create Project Task',
  {
    init: props<{ identifier: string; task: AddProjectTaskRequest }>(),
    success: props<{ task: TaskViewModel }>(),
  }
);

// Edit Task

export const editProjectTask = createAsyncAction(
  '[ProjectTasks] Edit Project Task',
  {
    init: props<{
      identifier: string;
      task: BoardViewTask | TaskViewModel | Partial<UpdateProjectTaskRequest>;
      silent?: boolean;
    }>(),
    success: props<{ task: TaskViewModel }>(),
  }
);

// Bulk Edit Tasks

export const bulkUpdateTasks = createAsyncAction(
  '[ProjectTasks] Bulk Update Tasks',
  {
    init: props<{ identifier: string; request: BulkUpdateTasksRequest }>(),
  }
);

// Delete Task

export const deleteProjectTask = createAsyncAction(
  '[ProjectTasks] Delete Project Task',
  {
    init: props<{ identifier: string; task: TaskViewModel }>(),
    success: props<{ taskId: number; identifier: string }>(),
  }
);

// Bulk Delete Tasks

export const bulkDeleteTasksAction = createAsyncAction(
  '[ProjectTasks] Bulk Delete Tasks',
  {
    init: props<{ identifier: string; ids: number[] }>(),
    success: props<{ taskIds: number[] }>(),
  }
);

// Task Selection

export const setSelectedTaskIds = createAction(
  '[ProjectTasks] Set Selected Task Ids',
  props<{ ids: number[] }>()
);

export const clearSelectedTaskIds = createAction(
  '[ProjectTasks] Clear Selected Task Ids'
);

// Select Task

export const selectTask = createAction(
  '[ProjectTasks] Select Task',
  props<{ task: TaskViewModel }>()
);

export const clearSelectedTask = createAction(
  '[ProjectTasks] Clear selected Task'
);

// Load Task Detail

export const loadTaskDetails = createAsyncAction(
  '[ProjectTasks] Load Task Detail',
  {
    init: props<{ systemId: string }>(),
    success: props<{ task: TaskViewModel }>(),
  }
);

// Clear Task detail

export const clearTaskDetail = createAction('[ProjectTasks] Clear Task detail');

// Set Inline Edit Active

export const setInlineEditActive = createAction(
  '[ProjectTasks] Set Inline Edit Active',
  props<{ active: boolean }>()
);

// Export Tasks

export const exportTasks = createAsyncAction('[ProjectTasks] Export Tasks', {
  success: props<{ reponse: FileResponse }>(),
});

// Import Tasks

export const importTasks = createAsyncAction('[ProjectTasks] Import Tasks', {
  init: props<{ boardIdentifier: string; file: File }>(),
});

// Delete Tag From Task

export const deleteTagFromTask = createAsyncAction(
  '[ProjectTasks] Delete Tag From Task',
  {
    init: props<{ identifier: string; systemId: string; tag: string }>(),
  }
);

// Add Tag To Task

export const addTagToTask = createAsyncAction(
  '[ProjectTasks] Add Tag To Task',
  {
    init: props<{ identifier: string; request: AddTagToTaskRequest }>(),
    success: props<{ tag: Tag }>(),
  }
);

// Load Activity

export const loadActivity = createAsyncAction('[ProjectTasks] Load Activity', {
  success: props<{ tags: Tag[] }>(),
});

// Filters

export const setSearchTerm = createAction(
  '[ProjectTasks] Set Search Term',
  props<{ term?: string | null }>()
);

export const toggleSelectedStatus = createAction(
  '[ProjectTasks] Toggle Selected Status',
  props<{ status: number }>()
);

export const toggleSelectedAssignee = createAction(
  '[ProjectTasks] Toggle Selected Assignee',
  props<{ assigneeId: string }>()
);
