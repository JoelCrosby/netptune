import { Project } from '@core/models/project';

export interface CoreState {
  currentProject?: Project;
}

export const initialState: CoreState = {};
