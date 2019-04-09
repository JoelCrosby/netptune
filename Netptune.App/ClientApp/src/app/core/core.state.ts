import { environment } from '@env/environment';
import { routerReducer, RouterReducerState } from '@ngrx/router-store';
import { ActionReducerMap, MetaReducer, createFeatureSelector, createSelector } from '@ngrx/store';
import { storeFreeze } from 'ngrx-store-freeze';
import { authReducer, AuthState } from './auth/store/auth.reducer';
import { debug } from './meta-reducers/debug.reducer';
import { initStateFromLocalStorage } from './meta-reducers/init-state-from-local-storage.reducer';
import { RouterStateUrl } from './router/router.state';

export const reducers: ActionReducerMap<AppState> = {
  auth: authReducer,
  router: routerReducer,
};

export const metaReducers: MetaReducer<AppState>[] = [initStateFromLocalStorage];

if (!environment.production) {
  metaReducers.unshift(storeFreeze);
  metaReducers.unshift(debug);
}

export const selectAuthState = createFeatureSelector<AppState, AuthState>('auth');

export const selectRouterState = createFeatureSelector<RouterReducerState<RouterStateUrl>>(
  'router'
);

export const selectPageTitle = createSelector(
  selectRouterState,
  (state: RouterReducerState<RouterStateUrl>) => state && state.state && state.state.title
);

export interface AppState {
  auth: AuthState;
  router: RouterReducerState<RouterStateUrl>;
}
