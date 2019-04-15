import { Action } from '@ngrx/store';
import { Workspace } from '@app/core/models/workspace';
import { Project } from '../models/project';

export enum CoreActionTypes {
  SelectWorkspace = '[Core] Select Workspace',
  SelectProject = '[Core] Select Project',
}

export class ActionSelectWorkspace implements Action {
  readonly type = CoreActionTypes.SelectWorkspace;

  constructor(readonly payload: Workspace) {}
}

export class ActionSelectProject implements Action {
  readonly type = CoreActionTypes.SelectProject;

  constructor(readonly payload: Project) {}
}

export type CoreActions = ActionSelectWorkspace | ActionSelectProject;
