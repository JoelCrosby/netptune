import { Action, createReducer, on } from '@ngrx/store';
import { adapter, initialState, ProjectsState } from './projects.model';
import * as actions from './projects.actions';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): ProjectsState => initialState),

  // Load Projects

  on(actions.loadProjects.init, (state): ProjectsState => ({
    ...state,
    loading: true,
  })),
  on(actions.loadProjects.fail, (state, { error }): ProjectsState => ({
    ...state,
    loadingError: error,
  })),
  on(actions.loadProjects.success, (state, { projects }): ProjectsState =>
    adapter.setAll(projects, {
      ...state,
      loading: false,
      loaded: true,
      currentProject: projects && projects[0],
    })
  ),

  // Create Project

  on(actions.createProject.init, (state): ProjectsState => ({
    ...state,
    loading: true,
  })),
  on(actions.createProject.fail, (state, { error }): ProjectsState => ({
    ...state,
    loadingError: error,
  })),
  on(actions.createProject.success, (state, { project }): ProjectsState =>
    adapter.addOne(project, {
      ...state,
      loadingCreate: false,
      currentProject: state.currentProject ?? project,
    })
  ),

  // Update Project

  on(actions.updateProject.init, (state): ProjectsState => ({
    ...state,
    projectUpdateLoading: true,
  })),
  on(actions.updateProject.fail, (state, { error }): ProjectsState => ({
    ...state,
    loadingError: error,
    projectUpdateLoading: false,
  })),
  on(actions.updateProject.success, (state): ProjectsState => ({
    ...state,
    projectUpdateLoading: false,
  })),

  // Select Project

  on(actions.selectProject, (state, { project }): ProjectsState => ({
    ...state,
    currentProject: project,
  })),

  // Delete Project

  on(actions.deleteProject.init, (state): ProjectsState => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteProject.fail, (state, { error }): ProjectsState => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteProject.success, (state, { projectId }): ProjectsState =>
    adapter.removeOne(projectId, {
      ...state,
      deleteState: { loading: false },
    })
  ),

  // Get Project Boards
  on(actions.getProjectBoards.init, (state): ProjectsState => ({
    ...state,
    projectBoardsLoading: true,
  })),
  on(actions.getProjectBoards.fail, (state): ProjectsState => ({
    ...state,
    projectBoardsLoading: false,
  })),
  on(actions.getProjectBoards.success, (state, { boards }): ProjectsState => ({
    ...state,
    projectBoards: boards,
  }))
);

export const projectsReducer = (
  state: ProjectsState | undefined,
  action: Action
): ProjectsState => reducer(state, action);
