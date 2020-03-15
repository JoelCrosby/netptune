import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './tasks.actions';
import { adapter, initialState, TasksState } from './tasks.model';

const reducer = createReducer(
  initialState,
  on(actions.loadProjectTasks, state => ({ ...state, loading: true })),
  on(actions.loadProjectTasksFail, (state, { error }) => ({
    ...state,
    loading: false,
    loadProjectsError: error,
  })),
  on(actions.loadProjectTasksSuccess, (state, { tasks }) =>
    adapter.setAll(tasks, {
      ...state,
      loading: false,
      loaded: true,
    })
  ),
  on(actions.createProjectTask, state => ({ ...state, loadingNewTask: true })),
  on(actions.createProjectTasksFail, (state, { error }) => ({
    ...state,
    loadingNewTask: false,
    createNewTaskError: error,
  })),
  on(actions.createProjectTasksSuccess, (state, { task }) =>
    adapter.addOne(task, {
      ...state,
      loadingNewTask: false,
      createdTask: task,
    })
  ),
  on(actions.editProjectTask, state => ({
    ...state,
    editState: { loading: true },
  })),
  on(actions.editProjectTasksFail, (state, { error }) => ({
    ...state,
    editState: { loading: false, error },
  })),
  on(actions.editProjectTasksSuccess, (state, { task }) =>
    adapter.upsertOne(task, {
      ...state,
      editState: { loading: false },
    })
  ),
  on(actions.deleteProjectTask, state => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteProjectTasksFail, (state, { error }) => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteProjectTasksSuccess, (state, { task }) =>
    adapter.removeOne(task.id, {
      ...state,
      deleteState: { loading: false },
    })
  ),
  on(actions.selectTask, (state, { task }) => ({
    ...state,
    selectedTask: task,
  })),
  on(actions.clearSelectedTask, state => ({
    ...state,
    selectedTask: undefined,
  })),
  on(actions.setInlineEditActive, (state, { active }) => ({
    ...state,
    inlineEditActive: active,
  }))
);

export function projectTasksReducer(
  state: TasksState | undefined,
  action: Action
) {
  return reducer(state, action);
}
