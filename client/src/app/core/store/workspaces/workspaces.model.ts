import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { Workspace } from '@core/models/workspace';
import { createEntityAdapter } from '@ngrx/entity';
import { IsSlugUniqueResponse } from '@core/models/is-slug-unique-response';
import { HttpErrorResponse } from '@angular/common/http';

export const adapter = createEntityAdapter<Workspace>();

export const initialState: WorkspacesState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
  loadingEdit: false,
  isSlugUniqueLoading: false,
});

export interface WorkspacesState extends AsyncEntityState<Workspace> {
  currentWorkspace?: Workspace;
  isSlugUnique?: IsSlugUniqueResponse;
  isSlugUniqueLoading: boolean;
  isSlugUniqueError?: HttpErrorResponse;
  loadingEdit: boolean;
  loadingEditError?: HttpErrorResponse | Error;
}
