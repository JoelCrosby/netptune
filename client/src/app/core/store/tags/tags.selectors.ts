import { createSelector } from '@ngrx/store';
import { selectTagsFeature } from '@core/core.state';
import { adapter, TagsState } from './tags.model';

const { selectAll } = adapter.getSelectors();

export const selectTags = createSelector(selectTagsFeature, selectAll);

export const selectTagsLoaded = createSelector(
  selectTagsFeature,
  (state: TagsState) => state.loaded
);
