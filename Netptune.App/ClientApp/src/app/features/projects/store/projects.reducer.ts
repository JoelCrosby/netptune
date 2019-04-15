import { ProjectsActions, ProjectsActionTypes } from './projects.actions';
import { Project } from '@app/core/models/project';

export interface ProjectsState {
  projects: Project[];
  loading: boolean;
  loaded: boolean;
  loadProjectsError?: any;
  createProjectError?: any;
  createProjectLoading: boolean;
}

export const initialState: ProjectsState = {
  projects: [],
  loading: false,
  loaded: false,
  createProjectLoading: false,
};

export function projectsReducer(state = initialState, action: ProjectsActions): ProjectsState {
  switch (action.type) {
    case ProjectsActionTypes.LoadProjects:
      return { ...state, loading: true };
    case ProjectsActionTypes.LoadProjectsFail:
      return { ...state, loading: false, loadProjectsError: action.payload };
    case ProjectsActionTypes.LoadProjectsSuccess:
      return { ...state, loading: false, loaded: true, projects: action.payload };
    case ProjectsActionTypes.CreateProject:
      return { ...state, createProjectLoading: true };
    case ProjectsActionTypes.CreateProjectFail:
      return { ...state, createProjectLoading: false, createProjectError: action.payload };
    case ProjectsActionTypes.CreateProjectSuccess:
      return {
        ...state,
        createProjectLoading: false,
        projects: [...state.projects, action.payload],
      };
    default:
      return state;
  }
}
