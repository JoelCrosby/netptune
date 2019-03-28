import { ProjectsActions, ProjectsActionTypes } from './projects.actions';
import { Project } from '@app/core/models/project';

export interface ProjectsState {
  projects: Project[];
  loading: boolean;
  loaded: boolean;
  loadProjectsError?: any;
}

export const initialState: ProjectsState = {
  projects: [],
  loading: false,
  loaded: false,
};

export function projectsReducer(state = initialState, action: ProjectsActions): ProjectsState {
  switch (action.type) {
    case ProjectsActionTypes.LoadProjects:
      return { ...state, loading: true };
    case ProjectsActionTypes.LoadProjectsFail:
      return { ...state, loading: false, loadProjectsError: action.payload };
    case ProjectsActionTypes.LoadProjectsSuccess:
      return { ...state, loading: false, loaded: true, projects: action.payload };
    default:
      return state;
  }
}
