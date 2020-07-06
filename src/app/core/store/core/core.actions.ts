import { createAction, props } from '@ngrx/store';
import { Project } from '@core/models/project';

export const selectProject = createAction(
  '[Core] Select Project',
  props<{ project: Project }>()
);
