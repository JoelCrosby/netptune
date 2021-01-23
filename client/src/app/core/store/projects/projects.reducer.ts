import { Action, createReducer, on } from '@ngrx/store';
import { adapter, initialState, ProjectsState } from './projects.model';
import * as actions from './projects.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),

  // Load Projects

  on(actions.loadProjects, (state) => ({ ...state, loading: true })),
  on(actions.loadProjectsFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.loadProjectsSuccess, (state, { projects }) =>
    adapter.setAll(projects, {
      ...state,
      loading: false,
      loaded: true,
      currentProject: projects && projects[0],
    })
  ),

  // Load Project Detail

  on(actions.loadProjectDetail, (state) => ({
    ...state,
    projectDetailLoading: true,
  })),
  on(actions.loadProjectDetailFail, (state, { error }) => ({
    ...state,
    loadingError: error,
    projectDetailLoading: false,
  })),
  on(actions.loadProjectDetailSuccess, (state, { project }) => ({
    ...state,
    projectDetail: project,
    projectDetailLoading: false,
  })),

  // Create Project

  on(actions.createProject, (state) => ({ ...state, loading: true })),
  on(actions.createProjectFail, (state, { error }) => ({
    ...state,
    loadingError: error,
  })),
  on(actions.createProjectSuccess, (state, { project }) =>
    adapter.addOne(project, {
      ...state,
      loadingCreate: false,
      currentProject: state.currentProject ?? project,
    })
  ),

  // Update Project

  on(actions.updateProject, (state) => ({
    ...state,
    projectUpdateLoading: true,
  })),
  on(actions.updateProjectFail, (state, { error }) => ({
    ...state,
    loadingError: error,
    projectUpdateLoading: false,
  })),
  on(actions.updateProjectSuccess, (state) => ({
    ...state,
    projectUpdateLoading: false,
  })),

  // Select Project

  on(actions.selectProject, (state, { project }) => ({
    ...state,
    currentProject: project,
  })),

  // Delete Project

  on(actions.deleteProject, (state) => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteProjectFail, (state, { error }) => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteProjectSuccess, (state, { response, projectId }) =>
    response.isSuccess
      ? adapter.removeOne(projectId, {
          ...state,
          deleteState: { loading: false },
        })
      : state
  ),

  // Get Project Boards
  on(actions.getProjectBoards, (state) => ({
    ...state,
    projectBoardsLoading: true,
  })),
  on(actions.getProjectBoardsFail, (state) => ({
    ...state,
    projectBoardsLoading: false,
  })),
  on(actions.getProjectBoardsSuccess, (state, { boards }) => ({
    ...state,
    projectBoards: boards,
  }))
);

export const projectsReducer = (
  state: ProjectsState | undefined,
  action: Action
): ProjectsState => reducer(state, action);
