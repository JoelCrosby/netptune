import { environment } from '@env/environment';
import {
  ActionReducerMap,
  createFeatureSelector,
  MetaReducer,
} from '@ngrx/store';
import { clearState } from './meta-reducers/clear-state';
import { debug } from './meta-reducers/debug.reducer';
import { initStateFromLocalStorage } from './meta-reducers/init-state-from-local-storage.reducer';
import type { AuthState } from './store/auth/auth.models';
import { authReducer } from './store/auth/auth.reducer';
import type { WorkspacesState } from './store/workspaces/workspaces.model';
import { workspacesReducer } from './store/workspaces/workspaces.reducer';

export const reducers: ActionReducerMap<Partial<AppState>> = {
  auth: authReducer,
  workspaces: workspacesReducer,
};

export const metaReducers: MetaReducer<Partial<AppState>>[] = [
  initStateFromLocalStorage,
  clearState,
];

if (!environment.production) {
  metaReducers.unshift(debug);
}

const selectFeature = <TState>(name: keyof AppState) =>
  createFeatureSelector<TState>(name);

export const selectAuthFeature = selectFeature<AuthState>('auth');
export const selectWorkspacesFeature =
  selectFeature<WorkspacesState>('workspaces');

export interface AppState {
  auth: AuthState;
  workspaces: WorkspacesState;
}
