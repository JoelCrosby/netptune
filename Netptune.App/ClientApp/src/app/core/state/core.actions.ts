import { Action } from '@ngrx/store';
import { Workspace } from '@app/core/models/workspace';

export enum CoreActionTypes {
  SelectWorkspace = '[Core] Select Workspace',
}

export class ActionSelectWorkspace implements Action {
  readonly type = CoreActionTypes.SelectWorkspace;

  constructor(readonly payload: Workspace) {}
}

export type CoreActions = ActionSelectWorkspace;
