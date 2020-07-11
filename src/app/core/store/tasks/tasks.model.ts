import { HttpErrorResponse } from '@angular/common/http';
import { TaskViewModel } from '@app/core/models/view-models/project-task-dto';
import { ProjectTask as TaskModel } from '@core/models/project-task';
import { ActionState, DefaultActionState } from '@core/types/action-state';
import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';
import { Comment } from '@core/models/comment';

export const adapter = createEntityAdapter<TaskViewModel>();

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
  comments: Comment[];
}
