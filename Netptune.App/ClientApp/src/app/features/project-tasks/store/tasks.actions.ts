import { ProjectTask, AddProjectTaskRequest } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[ProjectTasks] Clear State');

export const loadProjectTasks = createAction(
  '[ProjectTasks] Load ProjectTasks'
);

export const loadProjectTasksSuccess = createAction(
  '[ProjectTasks] Load ProjectTasks Success',
  props<{ tasks: TaskViewModel[] }>()
);

export const loadProjectTasksFail = createAction(
  '[ProjectTasks] Load ProjectTasks Fail',
  props<{ error: any }>()
);

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
  props<{ error: any }>()
);

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
  props<{ error: any }>()
);

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
  props<{ error: any }>()
);

export const selectTask = createAction(
  '[ProjectTasks] Select Task',
  props<{ task: ProjectTask }>()
);

export const clearSelectedTask = createAction(
  '[ProjectTasks] Clear selected Task'
);

export const setInlineEditActive = createAction(
  '[ProjectTasks] Set Inline Edit Active',
  props<{ active: boolean }>()
);
