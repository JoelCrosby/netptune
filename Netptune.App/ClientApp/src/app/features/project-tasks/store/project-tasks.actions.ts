import { Action } from '@ngrx/store';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { ProjectTask } from '@app/core/models/project-task';

export enum ProjectTasksActionTypes {
  LoadProjectTasks = '[ProjectTasks] Load ProjectTasks',
  LoadProjectTasksFail = '[ProjectTasks] Load ProjectTasks Fail',
  LoadProjectTasksSuccess = '[ProjectTasks] Load ProjectTasks Success',
  CreateProjectTask = '[ProjectTasks] Create Project Task',
  CreateProjectTaskFail = '[ProjectTasks] Create Project Task Fail',
  CreateProjectTaskSuccess = '[ProjectTasks] Create Project Task Success',
  EditProjectTask = '[ProjectTasks] Edit Project Task',
  EditProjectTaskFail = '[ProjectTasks] Edit Project Task Fail',
  EditProjectTaskSuccess = '[ProjectTasks] Edit Project Task Success',
  DeleteProjectTask = '[ProjectTasks] Delete Project Task',
  DeleteProjectTaskFail = '[ProjectTasks] Delete Project Task Fail',
  DeleteProjectTaskSuccess = '[ProjectTasks] Delete Project Task Success',
}

export class ActionLoadProjectTasks implements Action {
  readonly type = ProjectTasksActionTypes.LoadProjectTasks;
}

export class ActionLoadProjectTasksSuccess implements Action {
  readonly type = ProjectTasksActionTypes.LoadProjectTasksSuccess;

  constructor(readonly payload: ProjectTaskDto[]) {}
}

export class ActionLoadProjectTasksFail implements Action {
  readonly type = ProjectTasksActionTypes.LoadProjectTasksFail;

  constructor(readonly payload: any) {}
}

export class ActionCreateProjectTask implements Action {
  readonly type = ProjectTasksActionTypes.CreateProjectTask;

  constructor(readonly payload: ProjectTask) {}
}

export class ActionCreateProjectTasksSuccess implements Action {
  readonly type = ProjectTasksActionTypes.CreateProjectTaskSuccess;

  constructor(readonly payload: ProjectTaskDto) {}
}

export class ActionCreateProjectTasksFail implements Action {
  readonly type = ProjectTasksActionTypes.CreateProjectTaskFail;

  constructor(readonly payload: any) {}
}

export class ActionEditProjectTask implements Action {
  readonly type = ProjectTasksActionTypes.EditProjectTask;

  constructor(readonly payload: ProjectTaskDto) {}
}

export class ActionEditProjectTasksSuccess implements Action {
  readonly type = ProjectTasksActionTypes.EditProjectTaskSuccess;

  constructor(readonly payload: ProjectTaskDto) {}
}

export class ActionEditProjectTasksFail implements Action {
  readonly type = ProjectTasksActionTypes.EditProjectTaskFail;

  constructor(readonly payload: any) {}
}

export class ActionDeleteProjectTask implements Action {
  readonly type = ProjectTasksActionTypes.DeleteProjectTask;

  constructor(readonly payload: ProjectTaskDto) {}
}

export class ActionDeleteProjectTasksSuccess implements Action {
  readonly type = ProjectTasksActionTypes.DeleteProjectTaskSuccess;

  constructor(readonly payload: number) {}
}

export class ActionDeleteProjectTasksFail implements Action {
  readonly type = ProjectTasksActionTypes.DeleteProjectTaskFail;

  constructor(readonly payload: any) {}
}

export type ProjectTasksActions =
  | ActionLoadProjectTasks
  | ActionLoadProjectTasksFail
  | ActionLoadProjectTasksSuccess
  | ActionCreateProjectTask
  | ActionCreateProjectTasksSuccess
  | ActionCreateProjectTasksFail
  | ActionEditProjectTask
  | ActionEditProjectTasksFail
  | ActionEditProjectTasksSuccess
  | ActionDeleteProjectTask
  | ActionDeleteProjectTasksFail
  | ActionDeleteProjectTasksSuccess;
