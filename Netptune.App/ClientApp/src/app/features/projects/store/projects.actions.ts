import { Action } from '@ngrx/store';
import { Project } from '@app/core/models/project';

export enum ProjectsActionTypes {
  LoadProjects = '[Projects] Load Projects',
  LoadProjectsFail = '[Projects] Load Projects Fail',
  LoadProjectsSuccess = '[Projects] Load Projects Success ',
}

export class ActionLoadProjects implements Action {
  readonly type = ProjectsActionTypes.LoadProjects;
}

export class ActionLoadProjectsSuccess implements Action {
  readonly type = ProjectsActionTypes.LoadProjectsSuccess;

  constructor(readonly payload: Project[]) {}
}

export class ActionLoadProjectsFail implements Action {
  readonly type = ProjectsActionTypes.LoadProjectsFail;

  constructor(readonly payload: any) {}
}

export type ProjectsActions =
  | ActionLoadProjects
  | ActionLoadProjectsFail
  | ActionLoadProjectsSuccess;
