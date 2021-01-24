import { Injectable } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ConfirmationService } from '@core/services/confirmation.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { ROUTER_NAVIGATED } from '@ngrx/router-store';
import { Action } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, map, switchMap, withLatestFrom } from 'rxjs/operators';
import * as actions from './tags.actions';
import { TagsService } from './tags.service';

@Injectable()
export class TagsEffects {
  routerNavigated$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ROUTER_NAVIGATED),
      withLatestFrom(this.route.queryParamMap),
      map(([_, paramMap]) => {
        const selectedTags = paramMap.getAll('tags');
        return actions.setSelectedTags({ selectedTags });
      })
    )
  );

  loadProjectTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadTags, actions.deleteTagsSuccess),
      switchMap(() =>
        this.tagsService.get().pipe(
          map((tags) => actions.loadTagsSuccess({ tags })),
          catchError((error) => of(actions.loadTagsFail(error)))
        )
      )
    )
  );

  addTag$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.addTag),
      switchMap((action) =>
        this.tagsService.post({ tag: action.name }).pipe(
          unwrapClientReposne(),
          map((tag) => actions.addTagSuccess({ tag })),
          catchError((error) => of(actions.addTagFail(error)))
        )
      )
    )
  );

  addTagToTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.addTagToTask),
      switchMap((action) =>
        this.tagsService.postToTask(action.request).pipe(
          unwrapClientReposne(),
          map((tag) => actions.addTagToTaskSuccess({ tag })),
          catchError((error) => of(actions.addTagToTaskFail(error)))
        )
      )
    )
  );

  deleteTag$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteTags),
      switchMap((action) =>
        this.confirmation.open(DELETE_TAG_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.tagsService.delete({ tags: action.tags }).pipe(
              unwrapClientReposne(),
              map(() => actions.deleteTagsSuccess()),
              catchError((error) => of(actions.deleteTagsFail(error)))
            );
          })
        )
      )
    )
  );

  deleteTagFromTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.deleteTagFromTask),
      switchMap(({ systemId, tag }) =>
        this.tagsService.deleteFromTask({ systemId, tag }).pipe(
          unwrapClientReposne(),
          map(() => actions.deleteTagFromTaskSuccess()),
          catchError((error) => of(actions.deleteTagFromTaskFail(error)))
        )
      )
    )
  );

  editTag$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.editTag),
      switchMap(({ currentValue, newValue }) =>
        this.tagsService.patch({ currentValue, newValue }).pipe(
          unwrapClientReposne(),
          map((tag) => actions.editTagSuccess({ tag })),
          catchError((error) => of(actions.deleteTagFromTaskFail(error)))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private route: ActivatedRoute,
    private tagsService: TagsService,
    private confirmation: ConfirmationService
  ) {}
}

const DELETE_TAG_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete Tag',
  cancelLabel: 'Cancel',
  color: 'warn',
  title: 'Delete Tag',
  message: 'Are you sure you wish to delete this tag',
};
