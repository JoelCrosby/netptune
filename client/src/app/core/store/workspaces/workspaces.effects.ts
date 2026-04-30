import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { ConfirmationService } from '@core/services/confirmation.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Actions, createEffect, ofType, OnInitEffects } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { Action, Store } from '@ngrx/store';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { asyncScheduler, EMPTY, of } from 'rxjs';
import {
  catchError,
  filter,
  map,
  switchMap,
  tap,
  throttleTime,
} from 'rxjs/operators';
import { ProjectTasksHubService } from '../tasks/tasks.hub.service';
import * as actions from './workspaces.actions';
import { selectCurrentWorkspace } from './workspaces.selectors';
import { WorkspacesService } from './workspaces.service';
import { currentUser } from '@app/core/store/auth/auth.actions';

@Injectable()
export class WorkspacesEffects implements OnInitEffects {
  private store = inject(Store);
  private actions$ = inject<Actions<Action>>(Actions);
  private workspacesService = inject(WorkspacesService);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(SnackbarService);
  private hubService = inject(ProjectTasksHubService);

  init$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.initWorkspaces),
      concatLatestFrom(() => this.store.select(selectIsAuthenticated)),
      filter(([_, isAuth]) => isAuth),
      map(() => actions.loadWorkspaces())
    );
  });

  selectWorkspace$ = createEffect(
    () => {
      return this.actions$.pipe(
        ofType(actions.selectWorkspace),
        tap(() => void this.hubService.disconnect())
      );
    },
    { dispatch: false }
  );

  setCurrentWorkspace$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.setCurrentWorkspace),
      map(() => currentUser())
    );
  });

  loadWorkspaces$ = createEffect(
    ({ throttle = 800, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.loadWorkspaces),
        throttleTime(throttle, scheduler),
        switchMap(() =>
          this.workspacesService.get().pipe(
            map((workspaces) => actions.loadWorkspacesSuccess({ workspaces })),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadWorkspacesFail({ error }))
            )
          )
        )
      );
    }
  );

  createWorkspace$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createWorkspace),
      switchMap((action) =>
        this.workspacesService.post(action.request).pipe(
          unwrapClientReposne(),
          map((workspace) => actions.createWorkspaceSuccess({ workspace })),
          catchError((error: HttpErrorResponse) =>
            of(actions.createWorkspaceFail({ error }))
          )
        )
      )
    );
  });

  deleteWorkspace$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteWorkspace),
      switchMap(({ workspace }) =>
        this.confirmation.open(DELETE_WORKSPACE_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return EMPTY;

            return this.workspacesService.delete(workspace).pipe(
              tap(() => this.snackbar.open('Workspace deleted')),
              map(() => actions.deleteWorkspaceSuccess({ workspace })),
              catchError((error: HttpErrorResponse) =>
                of(actions.deleteWorkspaceFail({ error }))
              )
            );
          })
        )
      )
    );
  });

  editWorkspace$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.editWorkspace),
      switchMap((action) =>
        this.workspacesService.put(action.request).pipe(
          unwrapClientReposne(),
          map((workspace) => actions.editWorkspaceSuccess({ workspace })),
          catchError((error: HttpErrorResponse) =>
            of(actions.editWorkspaceFail({ error }))
          )
        )
      )
    );
  });

  toggleWorkspaceIsPublic$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.toggleWorkspaceIsPublic),
      concatLatestFrom(() => this.store.select(selectCurrentWorkspace)),
      filter(([_, workspace]) => !!workspace?.slug),
      map(([action, workspace]) => ({
        isPublic: action.isPublic,
        /* eslint-disable @typescript-eslint/no-non-null-assertion */
        slug: workspace!.slug!,
        metaInfo: workspace!.metaInfo ?? {},
        /* eslint-enable @typescript-eslint/no-non-null-assertion */
      })),
      switchMap((request) => {
        const confirmation = request.isPublic
          ? MARK_WORKSPACE_AS_PUBLIC_CONFIRMATION
          : MARK_WORKSPACE_AS_PRIVATE_CONFIRMATION;

        return this.confirmation.open(confirmation).pipe(
          switchMap((result) => {
            if (!result) return EMPTY;

            return this.workspacesService.put(request).pipe(
              unwrapClientReposne(),
              map((workspace) => actions.editWorkspaceSuccess({ workspace })),
              catchError((error: HttpErrorResponse) =>
                of(actions.editWorkspaceFail({ error }))
              )
            );
          })
        );
      })
    );
  });

  isSlugUnique$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.isSlugUniue),
      switchMap((action) =>
        this.workspacesService.isSlugUnique(action.slug).pipe(
          unwrapClientReposne(),
          map((response) => actions.isSlugUniueSuccess({ response })),
          catchError((error: HttpErrorResponse) =>
            of(actions.isSlugUniueFail({ error }))
          )
        )
      )
    );
  });

  ngrxOnInitEffects(): Action {
    return actions.initWorkspaces();
  }
}

const DELETE_WORKSPACE_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Delete',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to delete this Workspace?',
  title: 'Delete Workspace',
  color: 'warn',
};

const MARK_WORKSPACE_AS_PUBLIC_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Mark as Public',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to mark this Workspace as public?',
  title: 'Mark Workspace as Public',
  color: 'warn',
  confirmationCheckboxLabel:
    'I understand that this action will make all the content of the Workspace visible to everyone.',
};

const MARK_WORKSPACE_AS_PRIVATE_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: 'Mark as Private',
  cancelLabel: 'Cancel',
  message: 'Are you sure you want to mark this Workspace as private?',
  title: 'Mark Workspace as Private',
  color: 'warn',
  confirmationCheckboxLabel:
    'I understand that this action will make all the content of the Workspace only visible to its members.',
};
