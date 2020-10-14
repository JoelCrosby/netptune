import { Injectable } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {
  selectCurrentWorkspace,
  selectCurrentWorkspaceIdentifier,
} from '@core/store/workspaces/workspaces.selectors';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap, withLatestFrom } from 'rxjs/operators';
import * as actions from './tags.actions';
import { TagsService } from './tags.service';

@Injectable()
export class TagsEffects {
  loadProjectTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadTags),
      withLatestFrom(
        this.store.select(selectCurrentWorkspace),
        this.route.queryParamMap,
        this.route.queryParams
      ),
      switchMap(([_, workspace]) =>
        this.tagsService.get(workspace.slug).pipe(
          map((tags) => actions.loadTagsSuccess({ tags })),
          catchError((error) => of(actions.loadTagsFail(error)))
        )
      )
    )
  );

  addTagToTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.addTagToTask),
      switchMap((action) =>
        this.tagsService.post(action.request).pipe(
          unwrapClientReposne(),
          map((tag) => actions.addTagToTaskSuccess({ tag })),
          catchError((error) => of(actions.addTagToTaskFail(error)))
        )
      )
    )
  );

  deleteTagFromTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteTagFromTask),
      withLatestFrom(this.store.select(selectCurrentWorkspaceIdentifier)),
      switchMap(([{ systemId, tag }, workspace]) =>
        this.tagsService.deleteFromTask({ workspace, systemId, tag }).pipe(
          map((response) => actions.deleteTagFromTaskSuccess({ response })),
          catchError((error) => of(actions.deleteTagFromTaskFail(error)))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private store: Store,
    private route: ActivatedRoute,
    private tagsService: TagsService
  ) {}
}
