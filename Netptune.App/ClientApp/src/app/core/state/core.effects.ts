import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';

import { withLatestFrom, tap } from 'rxjs/operators';
import { CoreActionTypes, CoreActions } from './core.actions';
import { select, Store } from '@ngrx/store';
import { AppState, selectCoreState } from '../core.state';
import { LocalStorageService } from '../local-storage/local-storage.service';

export const CORE_KEY = 'CORE';

@Injectable()
export class CoreEffects {
  constructor(
    private actions$: Actions<CoreActions>,
    private store: Store<AppState>,
    private localStorageService: LocalStorageService
  ) {}

  @Effect({ dispatch: false })
  persistSettings = this.actions$.pipe(
    ofType(CoreActionTypes.SelectWorkspace),
    withLatestFrom(this.store.pipe(select(selectCoreState))),
    tap(([action, settings]) => this.localStorageService.setItem(CORE_KEY, settings))
  );
}
