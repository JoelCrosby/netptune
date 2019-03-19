import { ActionReducerMap, MetaReducer } from '@ngrx/store';
import { debug } from './meta-reducers/debug.reducer';
import { authReducer, AuthState } from './auth/store/auth.reducer';

export const reducers: ActionReducerMap<AppState> = {
  auth: authReducer,
};

export const metaReducers: MetaReducer<any>[] = [debug];

export interface AppState {
  auth: AuthState;
}
