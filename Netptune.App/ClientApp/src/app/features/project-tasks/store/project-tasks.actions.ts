import { Action } from '@ngrx/store';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';

export enum ProjectTasksActionTypes {
  LoadProjectTasks = '[ProjectTasks] Load ProjectTasks',
  LoadProjectTasksFail = '[ProjectTasks] Load ProjectTasks Fail',
  LoadProjectTasksSuccess = '[ProjectTasks] Load ProjectTasks Success',
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

export type ProjectTasksActions =
  | ActionLoadProjectTasks
  | ActionLoadProjectTasksFail
  | ActionLoadProjectTasksSuccess;
