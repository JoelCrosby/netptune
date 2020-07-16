import { AddProjectTaskRequest } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { createAction, props } from '@ngrx/store';
import { HttpErrorResponse } from '@angular/common/http';
import { AddCommentRequest } from '@app/core/models/requests/add-comment-request';
import { Comment } from '@core/models/comment';

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
  props<{ task: AddProjectTaskRequest }>()
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
  props<{ task: TaskViewModel; silent?: boolean }>()
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
  props<{ task: TaskViewModel }>()
);

export const deleteProjectTasksSuccess = createAction(
  '[ProjectTasks] Delete Project Task Success',
  props<{ task: TaskViewModel }>()
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
  props<{ comments: Comment[] }>()
);

export const loadCommentsFail = createAction(
  '[ProjectTasks] Load Comments Fail',
  props<{ error: HttpErrorResponse }>()
);

export const addComment = createAction(
  '[ProjectTasks] Add Comment',
  props<{ request: AddCommentRequest }>()
);

export const addCommentSuccess = createAction(
  '[ProjectTasks] Add Comment Success',
  props<{ comment: Comment }>()
);

export const addCommentFail = createAction(
  '[ProjectTasks] Add Comment Fail',
  props<{ error: HttpErrorResponse }>()
);