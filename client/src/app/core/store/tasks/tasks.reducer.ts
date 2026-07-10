import { Action, createReducer, on } from '@ngrx/store';
import * as actions from './tasks.actions';
import { adapter, initialState, TasksState } from './tasks.model';

const reducer = createReducer(
  initialState,
  on(actions.clearState, (): TasksState => initialState),

  // Load Tasks

  on(actions.loadProjectTasks.init, (state): TasksState => ({
    ...state,
    loading: true,
  })),
  on(actions.setProjectTasksPageSize, (state, { pageSize }): TasksState => ({
    ...state,
    pageSize,
    page: 1,
  })),
  on(actions.setProjectTasksPage, (state, { page }): TasksState => ({
    ...state,
    page,
  })),
  on(
    actions.hydrateProjectTaskFiltersFromRoute,
    (state, { term, assigneeIds, statuses }): TasksState => ({
      ...state,
      searchTerm: term,
      selectedAssignees: assigneeIds,
      selectedStatuses: statuses,
      page: 1,
    })
  ),
  on(actions.loadProjectTasks.fail, (state, { error }): TasksState => ({
    ...state,
    loading: false,
    loadProjectsError: error,
  })),
  on(
    actions.loadProjectTasks.success,
    (state, { tasks, page, pageSize, totalCount, totalPages }): TasksState =>
      adapter.setAll(tasks, {
        ...state,
        loading: false,
        loaded: true,
        page,
        pageSize,
        totalCount,
        totalPages,
      })
  ),
  // Create Task

  on(actions.createProjectTask.init, (state): TasksState => ({
    ...state,
    loadingNewTask: true,
  })),
  on(actions.createProjectTask.fail, (state, { error }): TasksState => ({
    ...state,
    loadingNewTask: false,
    createError: error,
  })),
  on(actions.createProjectTask.success, (state, { task }): TasksState =>
    adapter.addOne(task, {
      ...state,
      loadingNewTask: false,
      createdTask: task,
    })
  ),

  // Edit Task

  on(actions.editProjectTask.init, (state): TasksState => ({
    ...state,
    editState: { loading: true },
  })),
  on(actions.editProjectTask.fail, (state, { error }): TasksState => ({
    ...state,
    editState: { loading: false, error },
  })),
  on(actions.editProjectTask.success, (state, { task }): TasksState =>
    adapter.upsertOne(task, {
      ...state,
      editState: { loading: false },
      detailTask: task,
    })
  ),

  // Delete Task

  on(actions.deleteProjectTask.init, (state): TasksState => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.deleteProjectTask.fail, (state, { error }): TasksState => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.deleteProjectTask.success, (state, { taskId }): TasksState =>
    adapter.removeOne(taskId, {
      ...state,
      deleteState: { loading: false },
    })
  ),

  // Bulk Delete Tasks

  on(actions.bulkDeleteTasksAction.init, (state): TasksState => ({
    ...state,
    deleteState: { loading: true },
  })),
  on(actions.bulkDeleteTasksAction.fail, (state, { error }): TasksState => ({
    ...state,
    deleteState: { loading: false, error },
  })),
  on(actions.bulkDeleteTasksAction.success, (state, { taskIds }): TasksState =>
    adapter.removeMany(taskIds, {
      ...state,
      deleteState: { loading: false },
      selectedTaskIds: [],
    })
  ),

  // Task Selection

  on(actions.setSelectedTaskIds, (state, { ids }): TasksState => ({
    ...state,
    selectedTaskIds: ids,
  })),
  on(actions.clearSelectedTaskIds, (state): TasksState => ({
    ...state,
    selectedTaskIds: [],
  })),

  // Select Task

  on(actions.selectTask, (state, { task }): TasksState => ({
    ...state,
    selectedTask: task,
  })),
  on(actions.clearSelectedTask, (state): TasksState => ({
    ...state,
    selectedTask: undefined,
  })),

  // Set Inline Edit Active

  on(actions.setInlineEditActive, (state, { active }): TasksState => ({
    ...state,
    inlineEditActive: active,
  })),

  // Load Task Details

  on(actions.loadTaskDetails.success, (state, { task }): TasksState => ({
    ...state,
    detailTask: task,
  })),

  // Clear Task Detail

  on(actions.clearTaskDetail, (state): TasksState => ({
    ...state,
    detailTask: undefined,
    comments: [],
  })),

  // Filters

  on(actions.setSearchTerm, (state, { term }): TasksState => ({
    ...state,
    searchTerm: term,
  })),
  on(actions.toggleSelectedStatus, (state, { status }): TasksState => ({
    ...state,
    selectedStatuses: state.selectedStatuses.includes(status)
      ? state.selectedStatuses.filter((item) => item !== status)
      : [...state.selectedStatuses, status],
  })),
  on(actions.toggleSelectedAssignee, (state, { assigneeId }): TasksState => ({
    ...state,
    selectedAssignees: state.selectedAssignees.includes(assigneeId)
      ? state.selectedAssignees.filter((item) => item !== assigneeId)
      : [...state.selectedAssignees, assigneeId],
  }))
);

export const projectTasksReducer = (
  state: TasksState | undefined,
  action: Action
): TasksState => reducer(state, action);
