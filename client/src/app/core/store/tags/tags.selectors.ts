import { createSelector } from '@ngrx/store';
import { selectTagsFeature } from '@core/core.state';
import { adapter, TagsState } from './tags.model';
import { Tag } from '@core/models/tag';
import { Selected } from '@core/models/selected';

const { selectAll } = adapter.getSelectors();

export const selectTags = createSelector(selectTagsFeature, selectAll);

export const selectTagNames = createSelector(selectTags, (state: Tag[]) =>
  state.map((tag) => tag.name)
);

export const selectTagsLoaded = createSelector(
  selectTagsFeature,
  (state: TagsState) => state.loaded
);

export const selectTagsLoading = createSelector(
  selectTagsFeature,
  (state: TagsState) => state.loading
);

export const selectSelectedTags = createSelector(
  selectTagsFeature,
  (state: TagsState) => state.selectedTags
);

export const selectSelectedTagCount = createSelector(
  selectSelectedTags,
  (state: string[]) => state.length
);

export const selectTasksWithSelect = createSelector(
  selectTags,
  selectSelectedTags,
  (state: Tag[], selected: string[]): Selected<Tag>[] => {
    const selectedSet = new Set(selected);
    return state.map((tag) => ({
      ...tag,
      selected: selectedSet.has(tag.name),
    }));
  }
);
