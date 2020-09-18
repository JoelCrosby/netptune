import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import * as actions from './tags.actions';
import { TagsService } from './tags.service';
import { of } from 'rxjs';
import { withLatestFrom, switchMap, map, catchError } from 'rxjs/operators';

@Injectable()
export class TagsEffects {
  loadProjectTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadTags),
      withLatestFrom(this.store.select(selectCurrentWorkspace)),
      switchMap(([_, workspace]) =>
        this.tagsService.get(workspace.slug).pipe(
          map((tags) => actions.loadTagsSuccess({ tags })),
          catchError((error) => of(actions.loadTagsFail(error)))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private store: Store,
    private tagsService: TagsService
  ) {}
}
