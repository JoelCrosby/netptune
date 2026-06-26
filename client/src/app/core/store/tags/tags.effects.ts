import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ConfirmationService } from '@core/services/confirmation.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { ROUTER_NAVIGATED } from '@ngrx/router-store';
import { Action } from '@ngrx/store';
import { EMPTY, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import * as actions from './tags.actions';
import { TagsService } from './tags.service';

@Injectable()
export class TagsEffects {
  private actions$ = inject<Actions<Action>>(Actions);
  private route = inject(ActivatedRoute);
  private tagsService = inject(TagsService);
  private confirmation = inject(ConfirmationService);

  routerNavigated$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(ROUTER_NAVIGATED),
      concatLatestFrom(() => this.route.queryParamMap),
      map(([_, paramMap]) => {
        const selectedTags = paramMap.getAll('tags');
        return actions.setSelectedTags({ selectedTags });
      })
    );
  });

  loadProjectTasks$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.loadTags.init, actions.deleteTags.success),
      switchMap(() =>
        this.tagsService.get().pipe(
          map((tags) => actions.loadTags.success({ tags })),
          catchError((error: HttpErrorResponse) =>
            of(actions.loadTags.fail({ error }))
          )
        )
      )
    );
  });

  addTag$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.addTag.init),
      switchMap((action) =>
        this.tagsService.post({ tag: action.name }).pipe(
          unwrapClientReposne(),
          map((tag) => actions.addTag.success({ tag })),
          catchError((error: HttpErrorResponse) =>
            of(actions.addTag.fail({ error }))
          )
        )
      )
    );
  });

  addTagToTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.addTagToTask.init),
      switchMap((action) =>
        this.tagsService.postToTask(action.request).pipe(
          unwrapClientReposne(),
          map((tag) => actions.addTagToTask.success({ tag })),
          catchError((error: HttpErrorResponse) =>
            of(actions.addTagToTask.fail({ error }))
          )
        )
      )
    );
  });

  deleteTag$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteTags.init),
      switchMap((action) =>
        this.confirmation.open(DELETE_TAG_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return EMPTY;

            return this.tagsService.delete({ tags: action.tags }).pipe(
              unwrapClientReposne(),
              map(() => actions.deleteTags.success()),
              catchError((error: HttpErrorResponse) =>
                of(actions.deleteTags.fail({ error }))
              )
            );
          })
        )
      )
    );
  });

  deleteTagFromTask$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteTagFromTask.init),
      switchMap(({ systemId, tag }) =>
        this.tagsService.deleteFromTask({ systemId, tag }).pipe(
          unwrapClientReposne(),
          map(() => actions.deleteTagFromTask.success()),
          catchError((error: HttpErrorResponse) =>
            of(actions.deleteTagFromTask.fail({ error }))
          )
        )
      )
    );
  });

  editTag$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.editTag.init),
      switchMap(({ currentValue, newValue }) =>
        this.tagsService.patch({ currentValue, newValue }).pipe(
          unwrapClientReposne(),
          map((tag) => actions.editTag.success({ tag })),
          catchError((error: HttpErrorResponse) =>
            of(actions.editTag.fail({ error }))
          )
        )
      )
    );
  });
}

const DELETE_TAG_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete Tag',
  cancelLabel: 'Cancel',
  color: 'warn',
  title: 'Delete Tag',
  message: 'Are you sure you wish to delete this tag',
};
