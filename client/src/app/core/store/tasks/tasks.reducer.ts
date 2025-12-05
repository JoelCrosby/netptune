import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './tasks.actions';
import { adapter, initialState, TasksState } from './tasks.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): TasksState => initialState),

  // Load Tasks

  on(
    actions.loadProjectTasks,
    (state): TasksState => ({ ...state, loading: true })
  ),
  on(
    actions.loadProjectTasksFail,
    (state, { error }): TasksState => ({
      ...state,
      loading: false,
      loadProjectsError: error,
    })
  ),
  on(
    actions.loadProjectTasksSuccess,
    (state, { tasks }): TasksState =>
      adapter.setAll(tasks, {
        ...state,
        loading: false,
        loaded: true,
      })
  ),

  // Create Task

  on(
    actions.createProjectTask,
    (state): TasksState => ({
      ...state,
      loadingNewTask: true,
    })
  ),
  on(
    actions.createProjectTasksFail,
    (state, { error }): TasksState => ({
      ...state,
      loadingNewTask: false,
      createError: error,
    })
  ),
  on(
    actions.createProjectTasksSuccess,
    (state, { task }): TasksState =>
      adapter.addOne(task, {
        ...state,
        loadingNewTask: false,
        createdTask: task,
      })
  ),

  // Edit Task

  on(
    actions.editProjectTask,
    (state): TasksState => ({
      ...state,
      editState: { loading: true },
    })
  ),
  on(
    actions.editProjectTasksFail,
    (state, { error }): TasksState => ({
      ...state,
      editState: { loading: false, error },
    })
  ),
  on(
    actions.editProjectTasksSuccess,
    (state, { task }): TasksState =>
      adapter.upsertOne(task, {
        ...state,
        editState: { loading: false },
        detailTask: task,
      })
  ),

  // Delete Task

  on(
    actions.deleteProjectTask,
    (state): TasksState => ({
      ...state,
      deleteState: { loading: true },
    })
  ),
  on(
    actions.deleteProjectTasksFail,
    (state, { error }): TasksState => ({
      ...state,
      deleteState: { loading: false, error },
    })
  ),
  on(
    actions.deleteProjectTasksSuccess,
    (state, { taskId }): TasksState =>
      adapter.removeOne(taskId, {
        ...state,
        deleteState: { loading: false },
      })
  ),

  // Select Task

  on(
    actions.selectTask,
    (state, { task }): TasksState => ({
      ...state,
      selectedTask: task,
    })
  ),
  on(
    actions.clearSelectedTask,
    (state): TasksState => ({
      ...state,
      selectedTask: undefined,
    })
  ),

  // Set Inline Edit Active

  on(
    actions.setInlineEditActive,
    (state, { active }): TasksState => ({
      ...state,
      inlineEditActive: active,
    })
  ),

  // Load Task Details

  on(
    actions.loadTaskDetailsSuccess,
    (state, { task }): TasksState => ({
      ...state,
      detailTask: task,
    })
  ),

  // Load Comments

  on(
    actions.loadCommentsSuccess,
    (state, { comments }): TasksState => ({
      ...state,
      comments,
    })
  ),

  // Delete Comment

  on(
    actions.deleteCommentSuccess,
    (state, { commentId }): TasksState => ({
      ...state,
      comments: state.comments.filter((c) => c.id !== commentId),
    })
  ),

  // Add Comment

  on(
    actions.addCommentSuccess,
    (state, { comment }): TasksState => ({
      ...state,
      comments: [...state.comments, comment].sort((a, b) => {
        return (
          new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
      }),
    })
  ),

  // Clear Task Detail

  on(
    actions.clearTaskDetail,
    (state): TasksState => ({
      ...state,
      detailTask: undefined,
      comments: [],
    })
  )
);

export const projectTasksReducer = (
  state: TasksState | undefined,
  action: Action
): TasksState => reducer(state, action);
