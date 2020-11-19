import { HttpErrorResponse } from '@angular/common/http';
import { createAction, props } from '@ngrx/store';
import { BuildInfo } from './meta.model';

export const clearState = createAction('[Meta] Clear State');

// Load BuildInfo

export const loadBuildInfo = createAction('[Meta] Load BuildInfo');

export const loadBuildInfoSuccess = createAction(
  '[Meta] Load BuildInfo Success ',
  props<{ buildInfo: BuildInfo }>()
);

export const loadBuildInfoFail = createAction(
  '[Meta] Load BuildInfo Fail',
  props<{ error: HttpErrorResponse }>()
);
