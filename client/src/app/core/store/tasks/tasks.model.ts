import { HttpErrorResponse } from '@angular/common/http';
import { CommentViewModel } from '@core/models/comment';
import { ProjectTask as TaskModel } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ActionState, DEFAULT_ACTION_STATE } from '@core/types/action-state';
import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<TaskViewModel>();

export const initialState: TasksState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
  loadingNewTask: false,
  deleteState: DEFAULT_ACTION_STATE,
  editState: DEFAULT_ACTION_STATE,
  comments: [],
  searchTerm: null,
  selectedStatuses: [],
  selectedAssignees: [],
  selectedTaskIds: [],
  page: 1,
  pageSize: 50,
  totalCount: 0,
  totalPages: 1,
});

export interface TasksState extends AsyncEntityState<TaskViewModel> {
  loading: boolean;
  loaded: boolean;
  loadProjectsError?: HttpErrorResponse;
  loadingNewTask: boolean;
  createdTask?: TaskModel;
  deleteState: ActionState;
  editState: ActionState;
  selectedTask?: TaskViewModel;
  inlineEditActive?: boolean;
  detailTask?: TaskViewModel;
  comments: CommentViewModel[];
  searchTerm?: string | null;
  selectedStatuses: number[];
  selectedAssignees: string[];
  selectedTaskIds: number[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TaskListGroup {
  groupName: string;
  tasks: TaskViewModel[];
  header: string;
  statusId: number;
  emptyMessage: string;
}

export interface ProjectTasksFilter {
  search?: string | null;
  sprintId?: number;
  noSprint?: boolean;
  tags?: string[];
  statusIds?: number[];
  assignees?: string[];
  page?: number;
  pageSize?: number;
}
