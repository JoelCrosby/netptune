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

export const reducers: ActionReducerMap<Partial<AppState>> = {
  auth: authReducer,
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

export interface AppState {
  auth: AuthState;
}
