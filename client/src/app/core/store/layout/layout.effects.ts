import { Injectable } from '@angular/core';
import { Actions, OnInitEffects, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { MediaService, MediaSize } from '@core/services/media.service';
import { map, withLatestFrom, filter } from 'rxjs/operators';
import { setIsMobileView, closeSideMenu, openSideMenu } from './layout.actions';
import { selectIsMobileView } from './layout.selectors';
import { AppState } from '@core/core.state';

@Injectable()
export class LayoutEffects implements OnInitEffects {
  init$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType('[Layout]: Init'),
        map(() =>
          this.media.maxWidth(MediaSize.xs).subscribe({
            next: (matches) => {
              if (matches) {
                this.store.dispatch(closeSideMenu());
              } else {
                this.store.dispatch(openSideMenu());
              }

              this.store.dispatch(setIsMobileView({ isMobileView: matches }));
            },
          })
        )
      ),
    { dispatch: false }
  );

  routeChanged$ = createEffect(() =>
    this.actions$.pipe(
      ofType('@ngrx/router-store/navigation'),
      withLatestFrom(this.store.select(selectIsMobileView)),
      filter(([_, isMobileView]) => isMobileView),
      map(() => closeSideMenu())
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private media: MediaService,
    private store: Store<AppState>
  ) {}

  ngrxOnInitEffects(): Action {
    return { type: '[Layout]: Init' };
  }
}
