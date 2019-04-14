import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';

import { concatMap } from 'rxjs/operators';
import { EMPTY } from 'rxjs';
import { CoreActionTypes, CoreActions } from './core.actions';

@Injectable()
export class CoreEffects {
  @Effect()
  loadCores$ = this.actions$.pipe(
    ofType(CoreActionTypes.SelectWorkspace),
    /** An EMPTY observable only emits completion. Replace with your own observable API request */
    concatMap(() => EMPTY)
  );

  constructor(private actions$: Actions<CoreActions>) {}
}
