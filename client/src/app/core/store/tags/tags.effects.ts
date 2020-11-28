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
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { ConfirmationService } from '@core/services/confirmation.service';
import * as actions from './tags.actions';
import { TagsService } from './tags.service';

@Injectable()
export class TagsEffects {
  loadProjectTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.loadTags, actions.deleteTagsSuccess),
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

  addTag$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.addTag),
      withLatestFrom(this.store.select(selectCurrentWorkspaceIdentifier)),
      switchMap(([action, workspaceSlug]) =>
        this.tagsService.post({ tag: action.name, workspaceSlug }).pipe(
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
      withLatestFrom(this.store.select(selectCurrentWorkspaceIdentifier)),
      switchMap(([action, workspace]) =>
        this.confirmation.open(DELETE_TAG_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return of({ type: 'NO_ACTION' });

            return this.tagsService
              .delete({ workspace, tags: action.tags })
              .pipe(
                map((response) => actions.deleteTagsSuccess({ response })),
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
      withLatestFrom(this.store.select(selectCurrentWorkspaceIdentifier)),
      switchMap(([{ systemId, tag }, workspace]) =>
        this.tagsService.deleteFromTask({ workspace, systemId, tag }).pipe(
          map((response) => actions.deleteTagFromTaskSuccess({ response })),
          catchError((error) => of(actions.deleteTagFromTaskFail(error)))
        )
      )
    )
  );

  editTag$ = createEffect(() =>
    this.actions$.pipe(
      ofType(actions.editTag),
      withLatestFrom(this.store.select(selectCurrentWorkspaceIdentifier)),
      switchMap(([{ currentValue, newValue }, workspace]) =>
        this.tagsService.patch({ workspace, currentValue, newValue }).pipe(
          unwrapClientReposne(),
          map((tag) => actions.editTagSuccess({ tag })),
          catchError((error) => of(actions.deleteTagFromTaskFail(error)))
        )
      )
    )
  );

  constructor(
    private actions$: Actions<Action>,
    private store: Store,
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
