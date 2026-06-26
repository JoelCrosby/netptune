import { HttpErrorResponse } from '@angular/common/http';
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

export const loadProjectTasks = createAction(
  '[ProjectTasks] Load ProjectTasks'
);

export const setProjectTasksPageSize = createAction(
  '[ProjectTasks] Set ProjectTasks Page Size',
  props<{ pageSize: number }>()
);

export const setProjectTasksPage = createAction(
  '[ProjectTasks] Set ProjectTasks Page',
  props<{ page: number }>()
);

export const loadProjectTasksSuccess = createAction(
  '[ProjectTasks] Load ProjectTasks Success',
  props<{
    tasks: TaskViewModel[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  }>()
);

export const loadProjectTasksFail = createAction(
  '[ProjectTasks] Load ProjectTasks Fail',
  props<{ error: HttpErrorResponse }>()
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

export const createProjectTask = createAction(
  '[ProjectTasks] Create Project Task',
  props<{ identifier: string; task: AddProjectTaskRequest }>()
);

export const createProjectTasksSuccess = createAction(
  '[ProjectTasks] Create Project Task Success',
  props<{ task: TaskViewModel }>()
);

export const createProjectTasksFail = createAction(
  '[ProjectTasks] Create Project Task Fail',
  props<{ error: HttpErrorResponse }>()
);

// Edit Task

export const editProjectTask = createAction(
  '[ProjectTasks] Edit Project Task',
  props<{
    identifier: string;
    task: BoardViewTask | TaskViewModel | Partial<UpdateProjectTaskRequest>;
    silent?: boolean;
  }>()
);

export const editProjectTasksSuccess = createAction(
  '[ProjectTasks] Edit Project Task Success',
  props<{ task: TaskViewModel }>()
);

export const editProjectTasksFail = createAction(
  '[ProjectTasks] Edit Project Task Fail',
  props<{ error: HttpErrorResponse }>()
);

// Bulk Edit Tasks

export const bulkUpdateTasks = createAction(
  '[ProjectTasks] Bulk Update Tasks',
  props<{ identifier: string; request: BulkUpdateTasksRequest }>()
);

export const bulkUpdateTasksSuccess = createAction(
  '[ProjectTasks] Bulk Update Tasks Success'
);

export const bulkUpdateTasksFail = createAction(
  '[ProjectTasks] Bulk Update Tasks Fail',
  props<{ error: HttpErrorResponse }>()
);

// Delete Task

export const deleteProjectTask = createAction(
  '[ProjectTasks] Delete Project Task',
  props<{ identifier: string; task: TaskViewModel }>()
);

export const deleteProjectTasksSuccess = createAction(
  '[ProjectTasks] Delete Project Task Success',
  props<{ taskId: number }>()
);

export const deleteProjectTasksFail = createAction(
  '[ProjectTasks] Delete Project Task Fail',
  props<{ error: HttpErrorResponse }>()
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

export const loadTaskDetails = createAction(
  '[ProjectTasks] Load Task Detail',
  props<{ systemId: string }>()
);

export const loadTaskDetailsFail = createAction(
  '[ProjectTasks] Load Task Detail Fail',
  props<{ error: HttpErrorResponse }>()
);

export const loadTaskDetailsSuccess = createAction(
  '[ProjectTasks] Load Task Detail Success',
  props<{ task: TaskViewModel }>()
);

// Clear Task detail

export const clearTaskDetail = createAction('[ProjectTasks] Clear Task detail');

// Set Inline Edit Active

export const setInlineEditActive = createAction(
  '[ProjectTasks] Set Inline Edit Active',
  props<{ active: boolean }>()
);

// Export Tasks

export const exportTasks = createAction('[ProjectTasks] Export Tasks');

export const exportTasksSuccess = createAction(
  '[ProjectTasks] Export Tasks Success',
  props<{ reponse: FileResponse }>()
);

export const exportTasksFail = createAction(
  '[ProjectTasks] Export Tasks Fail',
  props<{ error: HttpErrorResponse }>()
);

// Import Tasks

export const importTasks = createAction(
  '[ProjectTasks] Import Tasks',
  props<{ boardIdentifier: string; file: File }>()
);

export const importTasksSuccess = createAction(
  '[ProjectTasks] Import Tasks Success'
);

export const importTasksFail = createAction(
  '[ProjectTasks] Import Tasks Fail',
  props<{ error: HttpErrorResponse }>()
);

// Delete Tag From Task

export const deleteTagFromTask = createAction(
  '[ProjectTasks] Delete Tag From Task',
  props<{ identifier: string; systemId: string; tag: string }>()
);

export const deleteTagFromTaskSuccess = createAction(
  '[ProjectTasks] Delete Tag From Task Success'
);

export const deleteTagFromTaskFail = createAction(
  '[ProjectTasks] Delete Tag From Task Fail',
  props<{ error: HttpErrorResponse }>()
);

// Add Tag To Task

export const addTagToTask = createAction(
  '[ProjectTasks] Add Tag To Task',
  props<{ identifier: string; request: AddTagToTaskRequest }>()
);

export const addTagToTaskSuccess = createAction(
  '[ProjectTasks] Add Tag To Task Success',
  props<{ tag: Tag }>()
);

export const addTagToTaskFail = createAction(
  '[ProjectTasks] Add Tag To Task Fail',
  props<{ error: HttpErrorResponse }>()
);

// Load Activity

export const loadActivity = createAction('[ProjectTasks] Load Activity');

export const loadActivitySuccess = createAction(
  '[ProjectTasks] Load Activity Success',
  props<{ tags: Tag[] }>()
);

export const loadActivityFail = createAction(
  '[ProjectTasks] Load Activity Fail',
  props<{ error: HttpErrorResponse }>()
);

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
