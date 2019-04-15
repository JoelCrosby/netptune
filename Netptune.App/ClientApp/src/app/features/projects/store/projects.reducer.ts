import { ProjectsActions, ProjectsActionTypes } from './projects.actions';
import { Project } from '@app/core/models/project';
import { EntityAdapter, createEntityAdapter, EntityState } from '@ngrx/entity';

export interface ProjectsState {
  projects: Projects;
  loading: boolean;
  loaded: boolean;
  loadProjectsError?: any;
  createProjectError?: any;
  createProjectLoading: boolean;
}

export const initialState: ProjectsState = {
  projects: { ids: [], entities: {} },
  loading: false,
  loaded: false,
  createProjectLoading: false,
};

export interface Projects extends EntityState<Project> {}

export const adapter: EntityAdapter<Project> = createEntityAdapter<Project>();

export function projectsReducer(state = initialState, action: ProjectsActions): ProjectsState {
  switch (action.type) {
    case ProjectsActionTypes.LoadProjects:
      return { ...state, loading: true };
    case ProjectsActionTypes.LoadProjectsFail:
      return { ...state, loading: false, loadProjectsError: action.payload };
    case ProjectsActionTypes.LoadProjectsSuccess:
      return {
        ...state,
        loading: false,
        loaded: true,
        projects: adapter.addAll(action.payload, state.projects),
      };
    case ProjectsActionTypes.CreateProject:
      return { ...state, createProjectLoading: true };
    case ProjectsActionTypes.CreateProjectFail:
      return { ...state, createProjectLoading: false, createProjectError: action.payload };
    case ProjectsActionTypes.CreateProjectSuccess:
      return {
        ...state,
        createProjectLoading: false,
        projects: adapter.addOne(action.payload, state.projects),
      };
    default:
      return state;
  }
}

const { selectIds, selectEntities, selectAll, selectTotal } = adapter.getSelectors();

export const selectProjectIds = selectIds;
export const selectProjectEntities = selectEntities;
export const selectAllProjects = selectAll;
export const selectProjectsTotal = selectTotal;
