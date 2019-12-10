import { AsyncEntityState } from '@app/core/entity/async-entity-state';
import { AppUser } from '@app/core/models/appuser';
import { createEntityAdapter } from '@ngrx/entity';

export const adapter = createEntityAdapter<AppUser>();

export const initialState = adapter.getInitialState({
  loading: false,
  loaded: false,
  loadingCreate: false,
});

export interface UsersState extends AsyncEntityState<AppUser> {}
