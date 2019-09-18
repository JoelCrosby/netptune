import { Action } from '@ngrx/store';
import { Workspace } from '@core/models/workspace';

export enum WorkspacesActionTypes {
  LoadWorkspaces = '[Workspaces] Load Workspaces',
  LoadWorkspacesFail = '[Workspaces] Load Workspaces Fail',
  LoadWorkspacesSuccess = '[Workspaces] Load Workspaces Success ',
  CreateWorkspace = '[Workspaces] Create Workspace',
  CreateWorkspaceFail = '[Workspaces] Create Workspace Fail',
  CreateWorkspaceSuccesss = '[Workspaces] Create Workspace Success',
}

export class ActionLoadWorkspaces implements Action {
  readonly type = WorkspacesActionTypes.LoadWorkspaces;
}

export class ActionLoadWorkspacesSuccess implements Action {
  readonly type = WorkspacesActionTypes.LoadWorkspacesSuccess;

  constructor(readonly payload: Workspace[]) {}
}

export class ActionLoadWorkspacesFail implements Action {
  readonly type = WorkspacesActionTypes.LoadWorkspacesFail;

  constructor(readonly payload: any) {}
}

export class ActionCreateWorkspaces implements Action {
  readonly type = WorkspacesActionTypes.CreateWorkspace;

  constructor(readonly payload: Workspace) {}
}

export class ActionCreateWorkspacesSuccess implements Action {
  readonly type = WorkspacesActionTypes.CreateWorkspaceSuccesss;

  constructor(readonly payload: Workspace) {}
}

export class ActionCreateWorkspacesFail implements Action {
  readonly type = WorkspacesActionTypes.CreateWorkspaceFail;

  constructor(readonly payload: any) {}
}

export type WorkspacesActions =
  | ActionLoadWorkspaces
  | ActionLoadWorkspacesFail
  | ActionLoadWorkspacesSuccess
  | ActionCreateWorkspaces
  | ActionCreateWorkspacesSuccess
  | ActionCreateWorkspacesFail;
