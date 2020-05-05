import { Action, createReducer, on } from '@ngrx/store';
import { adapter, initialState, ProjectsState } from './projects.model';
import * as actions from './projects.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),
  on(actions.loadProjects, (state) => ({ ...state, loading: true })),
  on(actions.loadProjectsFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.loadProjectsSuccess, (state, { projects }) =>
    adapter.setAll(projects, { ...state, loading: false, loaded: true })
  ),
  on(actions.createProject, (state) => ({ ...state, loading: true })),
  on(actions.createProjectFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.createProjectSuccess, (state, { project }) =>
    adapter.addOne(project, {
      ...state,
      loadingCreate: false,
    })
  ),
  on(actions.selectProject, (state, { project }) => ({
    ...state,
    currentProject: project,
  })),
  on(actions.deleteProject, (state) => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteProjectFail, (state, { error }) => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteProjectSuccess, (state, { project }) =>
    adapter.removeOne(project.id, {
      ...state,
      deleteState: { loading: false },
    })
  )
);

export function projectsReducer(
  state: ProjectsState | undefined,
  action: Action
) {
  return reducer(state, action);
}
