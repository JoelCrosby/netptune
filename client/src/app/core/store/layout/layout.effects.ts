import { Injectable, inject } from '@angular/core';
import { MediaService, MediaSize } from '@core/services/media.service';
import { Actions, OnInitEffects, createEffect, ofType } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { Action, Store } from '@ngrx/store';
import { filter, map, switchMap } from 'rxjs/operators';
import * as actions from './layout.actions';
import { selectIsMobileView } from './layout.selectors';

@Injectable()
export class LayoutEffects implements OnInitEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private media = inject(MediaService);
  private store = inject(Store);

  init$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.initLayout),
      switchMap(() => this.media.maxWidth(MediaSize.xs)),
      map((matches) => {
        if (matches) {
          return actions.closeSideMenu();
        } else {
          return actions.openSideMenu();
        }
      })
    );
  });

  setIsMobileView$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.initLayout),
      switchMap(() => this.media.maxWidth(MediaSize.xs)),
      map((matches) => actions.setIsMobileView({ isMobileView: matches }))
    );
  });

  routeChanged$ = createEffect(() => {
    return this.actions$.pipe(
      // eslint-disable-next-line @ngrx/prefer-action-creator-in-of-type
      ofType('@ngrx/router-store/navigation'),
      concatLatestFrom(() => this.store.select(selectIsMobileView)),
      filter(([_, isMobileView]) => isMobileView),
      map(() => actions.closeSideMenu())
    );
  });

  ngrxOnInitEffects(): Action {
    return actions.initLayout();
  }
}
