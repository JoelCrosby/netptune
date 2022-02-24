import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { asyncScheduler, of } from 'rxjs';
import { catchError, debounceTime, map, switchMap } from 'rxjs/operators';
import * as actions from './meta.actions';
import { MetaService } from './meta.service';

@Injectable()
export class MetaEffects {
  loadBuildInfo$ = createEffect(
    ({ debounce = 0, scheduler = asyncScheduler } = {}) =>
      this.actions$.pipe(
        ofType(actions.loadBuildInfo),
        debounceTime(debounce, scheduler),
        switchMap(() =>
          this.metaService.getBuildInfo().pipe(
            map((buildInfo) => actions.loadBuildInfoSuccess({ buildInfo })),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadBuildInfoFail({ error }))
            )
          )
        )
      )
  );

  constructor(
    private actions$: Actions<Action>,
    private metaService: MetaService
  ) {}
}
