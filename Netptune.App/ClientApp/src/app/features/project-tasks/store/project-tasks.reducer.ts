import { ProjectTasksActions, ProjectTasksActionTypes } from './project-tasks.actions';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { ProjectTask } from '@app/core/models/project-task';
import { EntityAdapter, createEntityAdapter, EntityState } from '@ngrx/entity';
import { ActionState, DefaultActionState } from '@app/core/types/action-state';

export interface ProjectTasksState {
  tasks: ProjectTasks;
  loading: boolean;
  loaded: boolean;
  loadProjectsError?: any;
  loadingNewTask: boolean;
  createNewTaskError?: boolean;
  createdTask?: ProjectTask;
  deleteState: ActionState;
  editState: ActionState;
}

export const initialState: ProjectTasksState = {
  tasks: { ids: [], entities: {} },
  loading: false,
  loaded: false,
  loadingNewTask: false,
  deleteState: DefaultActionState,
  editState: DefaultActionState,
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
      return {
        ...state,
        loadingNewTask: false,
        createdTask: action.payload,
        tasks: adapter.addOne(action.payload, state.tasks),
      };
    case ProjectTasksActionTypes.EditProjectTask:
      return { ...state, editState: { loading: true } };
    case ProjectTasksActionTypes.EditProjectTaskFail:
      return { ...state, editState: { loading: false, error: action.payload } };
    case ProjectTasksActionTypes.EditProjectTaskSuccess:
      return {
        ...state,
        editState: { loading: false },
        tasks: adapter.upsertOne(action.payload, state.tasks),
      };
    case ProjectTasksActionTypes.DeleteProjectTask:
      return { ...state, deleteState: { loading: true } };
    case ProjectTasksActionTypes.DeleteProjectTaskFail:
      return { ...state, deleteState: { loading: false, error: action.payload } };
    case ProjectTasksActionTypes.DeleteProjectTaskSuccess:
      return {
        ...state,
        deleteState: { loading: false },
        tasks: adapter.removeOne(action.payload, state.tasks),
      };
    default:
      return state;
  }
}

const { selectIds, selectEntities, selectAll, selectTotal } = adapter.getSelectors();

export const selectProjectTasksIds = selectIds;
export const selectProjectTaskEntities = selectEntities;
export const selectAllProjectTasks = selectAll;
export const selectProjectTasksTotal = selectTotal;
