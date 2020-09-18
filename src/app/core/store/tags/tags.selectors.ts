import { createSelector } from '@ngrx/store';
import { selectTagsFeature } from '@core/core.state';
import { adapter } from './tags.model';

const { selectAll } = adapter.getSelectors();

export const selectTasks = createSelector(selectTagsFeature, selectAll);
