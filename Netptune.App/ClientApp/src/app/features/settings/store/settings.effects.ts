import { OverlayContainer } from '@angular/cdk/overlay';
import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { select, Store } from '@ngrx/store';
import { merge, of } from 'rxjs';
import { tap, withLatestFrom } from 'rxjs/operators';
import { SettingsActions, SettingsActionTypes } from './settings.actions';
import { SettingsState } from './settings.model';
import { selectEffectiveTheme, selectSettingsState } from './settings.selectors';
import { LocalStorageService } from '@app/core/local-storage/local-storage.service';

const INIT = of('app-init-effect-trigger');

export const SETTINGS_KEY = 'SETTINGS';

@Injectable()
export class SettingsEffects {
  constructor(
    private actions$: Actions<SettingsActions>,
    private store: Store<SettingsState>,
    private overlayContainer: OverlayContainer,
    private localStorageService: LocalStorageService
  ) {}

  @Effect({ dispatch: false })
  persistSettings = this.actions$.pipe(
    ofType(SettingsActionTypes.CHANGE_THEME),
    withLatestFrom(this.store.pipe(select(selectSettingsState))),
    tap(([action, settings]) => this.localStorageService.setItem(SETTINGS_KEY, settings))
  );

  @Effect({ dispatch: false })
  updateTheme = merge(INIT, this.actions$.pipe(ofType(SettingsActionTypes.CHANGE_THEME))).pipe(
    withLatestFrom(this.store.pipe(select(selectEffectiveTheme))),
    tap(([action, effectiveTheme]) => {
      const classList = this.overlayContainer.getContainerElement().classList;
      const toRemove = Array.from(classList).filter((item: string) => item.includes('-theme'));
      if (toRemove.length) {
        classList.remove(...toRemove);
      }
      classList.add(effectiveTheme);
    })
  );
}
