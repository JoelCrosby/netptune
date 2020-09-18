import { HttpErrorResponse } from '@angular/common/http';
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
