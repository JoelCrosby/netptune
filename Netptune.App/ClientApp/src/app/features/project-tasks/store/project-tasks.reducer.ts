import { ProjectTasksActions, ProjectTasksActionTypes } from './project-tasks.actions';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { ProjectTask } from '@app/core/models/project-task';
import { EntityAdapter, createEntityAdapter, EntityState } from '@ngrx/entity';

export interface ProjectTasksState {
  tasks: ProjectTasks;
  loading: boolean;
  loaded: boolean;
  loadProjectsError?: any;
  loadingNewTask: boolean;
  createNewTaskError?: boolean;
  createdTask?: ProjectTask;
}

export const initialState: ProjectTasksState = {
  tasks: { ids: [], entities: {} },
  loading: false,
  loaded: false,
  loadingNewTask: false,
};

export interface ProjectTasks extends EntityState<ProjectTaskDto> {}

export const adapter: EntityAdapter<ProjectTaskDto> = createEntityAdapter<ProjectTaskDto>();

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
      return {
        ...state,
        loading: false,
        loaded: true,
        tasks: adapter.addAll(action.payload, state.tasks),
      };
    case ProjectTasksActionTypes.CreateProjectTask:
      return { ...state, loadingNewTask: true };
    case ProjectTasksActionTypes.CreateProjectTaskFail:
      return { ...state, loadingNewTask: false, createNewTaskError: action.payload };
    case ProjectTasksActionTypes.CreateProjectTaskSuccess:
      return { ...state, loadingNewTask: false, createdTask: action.payload };
    default:
      return state;
  }
}

const { selectIds, selectEntities, selectAll, selectTotal } = adapter.getSelectors();

export const selectProjectTasksIds = selectIds;
export const selectProjectTaskEntities = selectEntities;
export const selectAllProjectTasks = selectAll;
export const selectProjectTasksTotal = selectTotal;
