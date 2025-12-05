import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { createEntityAdapter } from '@ngrx/entity';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import {
  AsyncDataState,
  initialAsyncDataState,
} from '@core/types/async-data-state';

export const adapter = createEntityAdapter<ProjectViewModel>();

export const initialState: ProjectsState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
  currentProject: undefined,
  projectDetail: undefined,
  projectDetailLoading: true,
  projectUpdateLoading: false,
  projectBoards: [],
  projectBoardsLoading: false,
  deleteState: initialAsyncDataState(),
});

export interface ProjectsState extends AsyncEntityState<ProjectViewModel> {
  currentProject?: ProjectViewModel;
  projectDetail?: ProjectViewModel | null;
  projectDetailLoading: boolean;
  projectUpdateLoading: boolean;
  projectBoards: BoardViewModel[];
  projectBoardsLoading: boolean;
  deleteState: AsyncDataState;
}
