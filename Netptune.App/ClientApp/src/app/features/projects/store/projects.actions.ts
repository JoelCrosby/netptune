import { Action } from '@ngrx/store';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { Project, AddProjectRequest } from '@core/models/project';

export enum ProjectsActionTypes {
  LoadProjects = '[Projects] Load Projects',
  LoadProjectsFail = '[Projects] Load Projects Fail',
  LoadProjectsSuccess = '[Projects] Load Projects Success ',
  CreateProject = '[Projects] Create Projects',
  CreateProjectFail = '[Projects] Create Projects Fail',
  CreateProjectSuccess = '[Projects] Create Projects Success ',
}

export class ActionLoadProjects implements Action {
  readonly type = ProjectsActionTypes.LoadProjects;
}

export class ActionLoadProjectsSuccess implements Action {
  readonly type = ProjectsActionTypes.LoadProjectsSuccess;

  constructor(readonly payload: ProjectViewModel[]) {}
}

export class ActionLoadProjectsFail implements Action {
  readonly type = ProjectsActionTypes.LoadProjectsFail;

  constructor(readonly payload: any) {}
}

export class ActionCreateProject implements Action {
  readonly type = ProjectsActionTypes.CreateProject;

  constructor(readonly payload: AddProjectRequest) {}
}

export class ActionCreateProjectSuccess implements Action {
  readonly type = ProjectsActionTypes.CreateProjectSuccess;

  constructor(readonly payload: ProjectViewModel) {}
}

export class ActionCreateProjectFail implements Action {
  readonly type = ProjectsActionTypes.CreateProjectFail;

  constructor(readonly payload: any) {}
}

export type ProjectsActions =
  | ActionLoadProjects
  | ActionLoadProjectsFail
  | ActionLoadProjectsSuccess
  | ActionCreateProject
  | ActionCreateProjectFail
  | ActionCreateProjectSuccess;
