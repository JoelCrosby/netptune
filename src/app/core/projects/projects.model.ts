import { AsyncEntityState } from '@core/entity/async-entity-state';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<ProjectViewModel>();

export const initialState: ProjectsState = adapter.getInitialState({
  loading: false,
  loaded: false,
  loadingCreate: false,
  currentProject: undefined,
});

export interface ProjectsState extends AsyncEntityState<ProjectViewModel> {
  currentProject?: ProjectViewModel;
}
