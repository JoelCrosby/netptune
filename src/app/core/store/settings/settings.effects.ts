import { Injectable } from '@angular/core';
import { selectSettingsState } from '@app/core/core.state';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, select, Store } from '@ngrx/store';
import { merge, of } from 'rxjs';
import { tap, withLatestFrom } from 'rxjs/operators';
import * as actions from './settings.actions';
import { selectEffectiveTheme } from './settings.selectors';

const INIT = of('app-init-effect-trigger');

export const SETTINGS_KEY = 'SETTINGS';

@Injectable()
export class SettingsEffects {
  persistSettings$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(actions.changeTheme),
        withLatestFrom(this.store.pipe(select(selectSettingsState))),
        tap(([action, settings]) =>
          this.localStorageService.setItem(SETTINGS_KEY, settings)
        )
      ),
    { dispatch: false }
  );

  updateTheme$ = createEffect(
    () =>
      merge(INIT, this.actions$.pipe(ofType(actions.changeTheme))).pipe(
        withLatestFrom(this.store.pipe(select(selectEffectiveTheme))),
        tap(([action, effectiveTheme]) => {
          const classList = document.querySelector('body').classList;
          const toRemove = Array.from(classList).filter((item: string) =>
            item.includes('-theme')
          );
          if (toRemove.length) {
            classList.remove(...toRemove);
          }
          classList.add(effectiveTheme);
        })
      ),
    { dispatch: false }
  );

  constructor(
    private actions$: Actions<Action>,
    private store: Store,
    private localStorageService: LocalStorageService
  ) {}
}
