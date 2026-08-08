import { getErrorMessage } from '@core/util/error-message';
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

export const selectProfileError = createSelector(
  selectProfileFeature,
  (state) => state.loadProfileError
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
  (state: ProfileState) => {
    const error = state.changePasswordError;

    // Not the raw Error.message: unwrapClientReposne prefixes it with
    // "Server responded with failure: ", which getErrorMessage strips.
    return error ? getErrorMessage(error) : undefined;
  }
);

export const selectSetPasswordLoading = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.setPasswordLoading
);

export const selectSetPasswordError = createSelector(
  selectProfileFeature,
  (state: ProfileState) => {
    const error = state.setPasswordError;

    // Not the raw Error.message: unwrapClientReposne prefixes it with
    // "Server responded with failure: ", which getErrorMessage strips.
    return error ? getErrorMessage(error) : undefined;
  }
);

export const selectLoginProviders = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.loginProviders ?? []
);

// Undefined until the login methods have loaded, so the password card can hold
// off on choosing between its set and change modes.
export const selectHasPassword = createSelector(
  selectProfileFeature,
  (state: ProfileState) => state.hasPassword
);
