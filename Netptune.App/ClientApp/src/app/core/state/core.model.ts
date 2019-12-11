import { Project } from '@core/models/project';
import { Workspace } from '@core/models/workspace';

export interface CoreState {
  currentWorksapce?: Workspace;
  currentProject?: Project;
}

export const initialState: CoreState = {};
