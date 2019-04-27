import { createSelector, createFeatureSelector } from '@ngrx/store';
import { ProfileState } from './profile.reducer';
import { AppState } from '@app/core/core.state';

export const selectProfileFeature = createFeatureSelector<ProfileState>('profile');

export const selectProfile = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.profile
);

export const selectProfileLoading = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.loading
);

export const selectProfileLoaded = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.loaded
);

export interface State extends AppState {
  projects: ProfileState;
}
