import { createSelector, createFeatureSelector } from '@ngrx/store';
import { ProfileState } from './profile.model';
import { AppState } from '@core/core.state';

export const selectProfileFeature = createFeatureSelector<ProfileState>('profile');

export const selectProfile = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.profile
);

export const selectProfileLoading = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.loadProfileloading
);

export const selectProfileLoaded = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.profileloaded
);

export const selectUpdateProfileLoading = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.updateProfileLoading
);

export interface State extends AppState {
  projects: ProfileState;
}
