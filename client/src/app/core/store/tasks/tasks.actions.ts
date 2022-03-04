import { AddProjectTaskRequest } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { createAction, props } from '@ngrx/store';
import { HttpErrorResponse } from '@angular/common/http';
import { AddCommentRequest } from '@core/models/requests/add-comment-request';
import { CommentViewModel } from '@core/models/comment';
import { FileResponse } from '@core/types/file-response';
import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { Tag } from '@core/models/tag';
import { BoardViewTask } from '@core/models/view-models/board-view';
import { UpdateProjectTaskRequest } from '@core/models/requests/update-project-task-request';

export const clearState = createAction('[ProjectTasks] Clear State');

// Load Tasks

export const loadProjectTasks = createAction(
  '[ProjectTasks] Load ProjectTasks'
);

export const loadProjectTasksSuccess = createAction(
  '[ProjectTasks] Load ProjectTasks Success',
  props<{ tasks: TaskViewModel[] }>()
);

export const loadProjectTasksFail = createAction(
  '[ProjectTasks] Load ProjectTasks Fail',
  props<{ error: HttpErrorResponse }>()
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
    task: BoardViewTask | TaskViewModel | UpdateProjectTaskRequest;
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

// Comments

export const loadComments = createAction(
  '[ProjectTasks] Load Comments',
  props<{ systemId: string }>()
);

export const loadCommentsSuccess = createAction(
  '[ProjectTasks] Load Comments Success ',
  props<{ comments: CommentViewModel[] }>()
);

export const loadCommentsFail = createAction(
  '[ProjectTasks] Load Comments Fail',
  props<{ error: HttpErrorResponse }>()
);

// Add Comment

export const addComment = createAction(
  '[ProjectTasks] Add Comment',
  props<{ request: AddCommentRequest }>()
);

export const addCommentSuccess = createAction(
  '[ProjectTasks] Add Comment Success',
  props<{ comment: CommentViewModel }>()
);

export const addCommentFail = createAction(
  '[ProjectTasks] Add Comment Fail',
  props<{ error: HttpErrorResponse }>()
);

// Delete Comment

export const deleteComment = createAction(
  '[ProjectTasks] Delete Comment',
  props<{ commentId: number }>()
);

export const deleteCommentSuccess = createAction(
  '[ProjectTasks] Delete Comment Success',
  props<{ commentId: number }>()
);

export const deleteCommentFail = createAction(
  '[ProjectTasks] Delete Comment Fail',
  props<{ error: HttpErrorResponse }>()
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
