import { AsyncEntityState } from '@core/util/entity/async-entity-state';
import { AppUser } from '@core/models/appuser';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<AppUser>();

export const initialState = adapter.getInitialState({
  loading: true,
  loaded: false,
  loadingCreate: false,
});

export interface UsersState extends AsyncEntityState<AppUser> {}
