import { createAsyncAction } from '@core/util/create-async-action';
import { createAction, props } from '@ngrx/store';
import { BuildInfo } from './meta.model';

export const clearState = createAction('[Meta] Clear State');

// Load BuildInfo

export const loadBuildInfo = createAsyncAction('[Meta] Load BuildInfo', {
  success: props<{ buildInfo: BuildInfo }>(),
});
