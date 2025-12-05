import { Action, createReducer, on } from '@ngrx/store';
import { adapter, initialState, ProjectsState } from './projects.model';
import * as actions from './projects.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): ProjectsState => initialState),

  // Load Projects

  on(
    actions.loadProjects,
    (state): ProjectsState => ({ ...state, loading: true })
  ),
  on(
    actions.loadProjectsFail,
    (state, { error }): ProjectsState => ({
      ...state,
      loadingError: error,
    })
  ),
  on(
    actions.loadProjectsSuccess,
    (state, { projects }): ProjectsState =>
      adapter.setAll(projects, {
        ...state,
        loading: false,
        loaded: true,
        currentProject: projects && projects[0],
      })
  ),

  // Load Project Detail

  on(
    actions.loadProjectDetail,
    (state): ProjectsState => ({
      ...state,
      projectDetailLoading: true,
    })
  ),
  on(
    actions.loadProjectDetailFail,
    (state, { error }): ProjectsState => ({
      ...state,
      loadingError: error,
      projectDetailLoading: false,
    })
  ),
  on(
    actions.loadProjectDetailSuccess,
    (state, { project }): ProjectsState => ({
      ...state,
      projectDetail: project,
      projectDetailLoading: false,
    })
  ),

  // Clear Project Detail

  on(
    actions.clearProjectDetail,
    (state): ProjectsState => ({
      ...state,
      projectDetail: null,
      projectDetailLoading: true,
    })
  ),

  // Create Project

  on(
    actions.createProject,
    (state): ProjectsState => ({ ...state, loading: true })
  ),
  on(
    actions.createProjectFail,
    (state, { error }): ProjectsState => ({
      ...state,
      loadingError: error,
    })
  ),
  on(
    actions.createProjectSuccess,
    (state, { project }): ProjectsState =>
      adapter.addOne(project, {
        ...state,
        loadingCreate: false,
        currentProject: state.currentProject ?? project,
      })
  ),

  // Update Project

  on(
    actions.updateProject,
    (state): ProjectsState => ({
      ...state,
      projectUpdateLoading: true,
    })
  ),
  on(
    actions.updateProjectFail,
    (state, { error }): ProjectsState => ({
      ...state,
      loadingError: error,
      projectUpdateLoading: false,
    })
  ),
  on(
    actions.updateProjectSuccess,
    (state): ProjectsState => ({
      ...state,
      projectUpdateLoading: false,
    })
  ),

  // Select Project

  on(
    actions.selectProject,
    (state, { project }): ProjectsState => ({
      ...state,
      currentProject: project,
    })
  ),

  // Delete Project

  on(
    actions.deleteProject,
    (state): ProjectsState => ({
      ...state,
      deleteState: { loading: true },
    })
  ),
  on(
    actions.deleteProjectFail,
    (state, { error }): ProjectsState => ({
      ...state,
      deleteState: { loading: false, error },
    })
  ),
  on(
    actions.deleteProjectSuccess,
    (state, { projectId }): ProjectsState =>
      adapter.removeOne(projectId, {
        ...state,
        deleteState: { loading: false },
      })
  ),

  // Get Project Boards
  on(
    actions.getProjectBoards,
    (state): ProjectsState => ({
      ...state,
      projectBoardsLoading: true,
    })
  ),
  on(
    actions.getProjectBoardsFail,
    (state): ProjectsState => ({
      ...state,
      projectBoardsLoading: false,
    })
  ),
  on(
    actions.getProjectBoardsSuccess,
    (state, { boards }): ProjectsState => ({
      ...state,
      projectBoards: boards,
    })
  )
);

export const projectsReducer = (
  state: ProjectsState | undefined,
  action: Action
): ProjectsState => reducer(state, action);
