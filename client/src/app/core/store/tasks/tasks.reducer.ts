import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './tasks.actions';
import { adapter, initialState, TasksState } from './tasks.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, () => initialState),

  // Load Tasks

  on(actions.loadProjectTasks, (state) => ({ ...state, loading: true })),
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

  // Create Task

  on(actions.createProjectTask, (state) => ({
    ...state,
    loadingNewTask: true,
  })),
  on(actions.createProjectTasksFail, (state, { error }) => ({
    ...state,
    loadingNewTask: false,
    createError: error,
  })),
  on(actions.createProjectTasksSuccess, (state, { task }) =>
    adapter.addOne(task, {
      ...state,
      loadingNewTask: false,
      createdTask: task,
    })
  ),

  // Edit Task

  on(actions.editProjectTask, (state) => ({
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
      detailTask: task,
    })
  ),

  // Delete Task

  on(actions.deleteProjectTask, (state) => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteProjectTasksFail, (state, { error }) => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteProjectTasksSuccess, (state, { taskId }) =>
    adapter.removeOne(taskId, {
      ...state,
      deleteState: { loading: false },
    })
  ),

  // Select Task

  on(actions.selectTask, (state, { task }) => ({
    ...state,
    selectedTask: task,
  })),
  on(actions.clearSelectedTask, (state) => ({
    ...state,
    selectedTask: undefined,
  })),

  // Set Inline Edit Active

  on(actions.setInlineEditActive, (state, { active }) => ({
    ...state,
    inlineEditActive: active,
  })),

  // Load Task Details

  on(actions.loadTaskDetailsSuccess, (state, { task }) => ({
    ...state,
    detailTask: task,
  })),

  // Load Comments

  on(actions.loadCommentsSuccess, (state, { comments }) => ({
    ...state,
    comments,
  })),

  // Delete Comment

  on(actions.deleteCommentSuccess, (state, { commentId }) => ({
    ...state,
    comment: state.comments.filter((c) => c.id !== commentId),
  })),

  // Add Comment

  on(actions.addCommentSuccess, (state, { comment }) => ({
    ...state,
    comments: [...state.comments, comment].sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    ),
  })),

  // Clear Task Detail

  on(actions.clearTaskDetail, (state) => ({
    ...state,
    detailTask: undefined,
    comments: [],
  }))
);

export const projectTasksReducer = (
  state: TasksState | undefined,
  action: Action
): TasksState => reducer(state, action);
