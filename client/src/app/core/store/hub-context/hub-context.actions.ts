import { createAction, props } from '@ngrx/store';

export const clearState = createAction('[Hub-Context] Clear State');

// Set Current Group ID

export const setCurrentGroupId = createAction(
  '[Hub-Context] Set Current Group ID',
  props<{ groupId: string | null }>()
);
