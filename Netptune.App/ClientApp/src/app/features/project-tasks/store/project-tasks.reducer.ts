import { ProjectTasksActions, ProjectTasksActionTypes } from './project-tasks.actions';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';

export interface ProjectTasksState {
  tasks: ProjectTaskDto[];
  loading: boolean;
  loaded: boolean;
  loadProjectsError?: any;
}

export const initialState: ProjectTasksState = {
  tasks: [],
  loading: false,
  loaded: false,
};

export function projectTasksReducer(
  state = initialState,
  action: ProjectTasksActions
): ProjectTasksState {
  switch (action.type) {
    case ProjectTasksActionTypes.LoadProjectTasks:
      return { ...state, loading: true };
    case ProjectTasksActionTypes.LoadProjectTasksFail:
      return { ...state, loading: false, loadProjectsError: action.payload };
    case ProjectTasksActionTypes.LoadProjectTasksSuccess:
      return { ...state, loading: false, loaded: true, tasks: action.payload };
    default:
      return state;
  }
}
