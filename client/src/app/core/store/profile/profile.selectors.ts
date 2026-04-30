import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ProfileState } from './profile.model';

export const selectProfileFeature =
  createFeatureSelector<ProfileState>('profile');

export const selectProfile = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.profile
);

export const selectProfileLoading = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.loadProfileloading && !state.profileloaded
);

export const selectProfileLoaded = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.profileloaded
);

export const selectUpdateProfileLoading = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.updateProfileLoading
);

export const selectChangePasswordLoading = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.changePasswordLoading
);

export const selectChangePasswordError = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.changePasswordError?.message
);
