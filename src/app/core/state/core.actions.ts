import { createAction, props } from '@ngrx/store';
import { Project } from '../models/project';

export const selectProject = createAction(
  '[Core] Select Project',
  props<{ project: Project }>()
);
