import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { Tag } from '@core/models/tag';
import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[Tags] Clear State');

// Load Tags

export const loadTags = createAsyncAction('[Tags] Load Tags', {
  success: props<{ tags: Tag[] }>(),
});

// Sync selection action

export const toggleSelectedTag = createAction(
  '[Tags] Toggle Selected Tag',
  props<{ tag: string }>()
);

// Add Tag

export const addTag = createAsyncAction('[Tags] Add Tag', {
  init: props<{ name: string }>(),
  success: props<{ tag: Tag }>(),
});

// Add Tag To Task

export const addTagToTask = createAsyncAction('[Tags] Add Tag To Task', {
  init: props<{ request: AddTagToTaskRequest }>(),
  success: props<{ tag: Tag }>(),
});

// Delete Tag

export const deleteTags = createAsyncAction('[Tags] Delete Tags', {
  init: props<{ tags: string[] }>(),
});

// Delete Tag From Task

export const deleteTagFromTask = createAsyncAction(
  '[Tags] Delete Tag From Task',
  {
    init: props<{ systemId: string; tag: string }>(),
  }
);

// Edit Tag

export const editTag = createAsyncAction('[Tags] Edit Tag', {
  init: props<{ currentValue: string; newValue: string }>(),
  success: props<{ tag: Tag }>(),
});

// Set Selected Tags

export const setSelectedTags = createAction(
  '[Tags] Set Selected Tags',
  props<{ selectedTags: string[] }>()
);
