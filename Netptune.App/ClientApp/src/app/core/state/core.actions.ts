import { Workspace } from '@core/models/workspace';
import { createAction, props } from '@ngrx/store';
import { Project } from '../models/project';

export const selectWorkspace = createAction(
  '[Core] Select Workspace',
  props<{ workspace: Workspace }>()
);

export const selectProject = createAction(
  '[Core] Select Project',
  props<{ project: Project }>()
);
