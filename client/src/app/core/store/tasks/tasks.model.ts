import { HttpErrorResponse } from '@angular/common/http';
import { TaskStatus } from '@core/enums/project-task-status';
import { CommentViewModel } from '@core/models/comment';
import { ProjectTask as TaskModel } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ActionState, DefaultActionState } from '@core/types/action-state';
import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<TaskViewModel>({
  sortComparer: (a, b) => new Date(b.updatedAt).getUTCMilliseconds() - new Date(a.updatedAt).getUTCMilliseconds(),
});

export const initialState: TasksState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
  loadingNewTask: false,
  deleteState: DefaultActionState,
  editState: DefaultActionState,
  comments: [],
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
}

export interface TaskListGroup {
  groupName: string;
  tasks: TaskViewModel[];
  header: string;
  status: TaskStatus;
  emptyMessage: string;
}
