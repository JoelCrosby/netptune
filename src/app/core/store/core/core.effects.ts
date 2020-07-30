import { Injectable } from '@angular/core';
import { selectCoreState } from '@core/core.state';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { Action, select, Store } from '@ngrx/store';
import { tap, withLatestFrom } from 'rxjs/operators';
import * as actions from './core.actions';

export const CORE_KEY = 'CORE';

@Injectable()
export class CoreEffects {
  constructor(
    private actions$: Actions<Action>,
    private store: Store,
    private localStorageService: LocalStorageService
  ) {}

  @Effect({ dispatch: false })
  persistSettings = this.actions$.pipe(
    ofType(actions.selectProject),
    withLatestFrom(this.store.pipe(select(selectCoreState))),
    tap(([action, settings]) =>
      this.localStorageService.setItem(CORE_KEY, settings)
    )
  );
}
