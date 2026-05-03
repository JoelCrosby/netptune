import { HttpErrorResponse } from '@angular/common/http';
import { SprintStatus } from '@core/enums/sprint-status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { ActionState, DEFAULT_ACTION_STATE } from '@core/types/action-state';
import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<SprintViewModel>();

export const initialState: SprintsState = adapter.getInitialState({
  loading: false,
  loaded: false,
  loadingCreate: false,
  currentSprints: [],
  currentSprintsLoading: false,
  currentSprintsLoaded: false,
  detail: undefined,
  detailLoading: false,
  availableTasks: [],
  availableTasksLoading: false,
  filter: {},
  createState: DEFAULT_ACTION_STATE,
  updateState: DEFAULT_ACTION_STATE,
  deleteState: DEFAULT_ACTION_STATE,
});

export interface SprintFilter {
  projectId?: number;
  status?: SprintStatus;
  take?: number;
}

export interface SprintsState extends AsyncEntityState<SprintViewModel> {
  loading: boolean;
  loaded: boolean;
  loadingError?: HttpErrorResponse;
  currentSprints: SprintViewModel[];
  currentSprintsLoading: boolean;
  currentSprintsLoaded: boolean;
  detail?: SprintDetailViewModel;
  detailLoading: boolean;
  availableTasks: TaskViewModel[];
  availableTasksLoading: boolean;
  filter: SprintFilter;
  createState: ActionState;
  updateState: ActionState;
  deleteState: ActionState;
}
