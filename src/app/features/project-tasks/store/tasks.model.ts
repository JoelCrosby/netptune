import { AsyncEntityState } from '@app/core/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';
import { TaskViewModel } from '@app/core/models/view-models/project-task-dto';
import { ActionState, DefaultActionState } from '@app/core/types/action-state';
import { ProjectTask as TaskModel } from '@app/core/models/project-task';

export const adapter = createEntityAdapter<TaskViewModel>();

export const initialState: TasksState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
  loadingNewTask: false,
  deleteState: DefaultActionState,
  editState: DefaultActionState,
});

export interface TasksState extends AsyncEntityState<TaskViewModel> {
  loading: boolean;
  loaded: boolean;
  loadProjectsError?: any;
  loadingNewTask: boolean;
  createNewTaskError?: boolean;
  createdTask?: TaskModel;
  deleteState: ActionState;
  editState: ActionState;
  selectedTask?: TaskViewModel;
  inlineEditActive?: boolean;
  detailTask?: TaskViewModel;
}
