import { selectMetaFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { MetaState } from './meta.model';

export const selectBuildInfo = createSelector(
  selectMetaFeature,
  (state: MetaState) => state.buildInfo
);
