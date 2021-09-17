import { createSelector } from '@ngrx/store';
import { selectHubContextFeature } from '@core/core.state';
import { HubContextState } from './hub-context.reducer';

export const selectCurrentHubGroupId = createSelector(
  selectHubContextFeature,
  (state: HubContextState) => state.groupId
);

export const selectIsWorkspaceGroup = createSelector(
  selectCurrentHubGroupId,
  (state?: string | null) => state?.startsWith('[workspace]')
);
