import { Injectable, inject } from '@angular/core';
import { selectSettingsFeature } from '@core/core.state';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { merge, of } from 'rxjs';
import { tap, withLatestFrom } from 'rxjs/operators';
import * as actions from './settings.actions';
import { selectEffectiveTheme } from './settings.selectors';

const INIT = of('app-init-effect-trigger');

export const SETTINGS_KEY = 'SETTINGS';

@Injectable()
export class SettingsEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private store = inject(Store);
  private localStorageService = inject(LocalStorageService);

  persistSettings$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(actions.changeTheme),
        withLatestFrom(this.store.select(selectSettingsFeature)),
        tap(([_, settings]) =>
          this.localStorageService.setItem(SETTINGS_KEY, settings)
        )
      ),
    { dispatch: false }
  );

  updateTheme$ = createEffect(
    () =>
      merge(INIT, this.actions$.pipe(ofType(actions.changeTheme))).pipe(
        withLatestFrom(this.store.select(selectEffectiveTheme)),
        tap(([_, effectiveTheme]) => {
          const classList = document.documentElement.classList;
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
}
