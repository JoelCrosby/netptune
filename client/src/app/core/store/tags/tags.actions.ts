import { HttpErrorResponse } from '@angular/common/http';
import { ClientResponse } from '@core/models/client-response';
import { AddTagRequest } from '@core/models/requests/add-tag-request';
import { Tag } from '@core/models/tag';
import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[Tags] Clear State');

// Load Tags

export const loadTags = createAction('[Tags] Load Tags');

export const loadTagsSuccess = createAction(
  '[Tags] Load Tags Success',
  props<{ tags: Tag[] }>()
);

export const loadTagsFail = createAction(
  '[Tags] Load Tags Fail',
  props<{ error: HttpErrorResponse }>()
);

export const toggleSelectedTag = createAction(
  '[Tags] Toggle Selected Tag',
  props<{ tag: string }>()
);

// Add Tag To Task

export const addTagToTask = createAction(
  '[Tags] Add Tag To Task',
  props<{ request: AddTagRequest }>()
);

export const addTagToTaskSuccess = createAction(
  '[Tags] Add Tag To Task Success',
  props<{ tag: Tag }>()
);

export const addTagToTaskFail = createAction(
  '[Tags] Add Tag To Task Fail',
  props<{ error: HttpErrorResponse }>()
);

// Delete Tag From Task

export const deleteTagFromTask = createAction(
  '[Tags] Delete Tag From Task',
  props<{ systemId: string; tag: string }>()
);

export const deleteTagFromTaskSuccess = createAction(
  '[Tags] Delete Tag From Task Success',
  props<{ response: ClientResponse }>()
);

export const deleteTagFromTaskFail = createAction(
  '[Tags] Delete Tag From Task Fail',
  props<{ error: HttpErrorResponse }>()
);
