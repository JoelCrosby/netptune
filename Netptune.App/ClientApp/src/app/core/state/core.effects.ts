import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { Action, select, Store } from '@ngrx/store';
import { tap, withLatestFrom } from 'rxjs/operators';
import { AppState, selectCoreState } from '../core.state';
import { LocalStorageService } from '../local-storage/local-storage.service';
import * as actions from './core.actions';

export const CORE_KEY = 'CORE';

@Injectable()
export class CoreEffects {
  constructor(
    private actions$: Actions<Action>,
    private store: Store<AppState>,
    private localStorageService: LocalStorageService
  ) {}

  @Effect({ dispatch: false })
  persistSettings = this.actions$.pipe(
    ofType(actions.selectWorkspace),
    withLatestFrom(this.store.pipe(select(selectCoreState))),
    tap(([action, settings]) =>
      this.localStorageService.setItem(CORE_KEY, settings)
    )
  );
}
