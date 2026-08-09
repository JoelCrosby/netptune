import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { ConfirmationService } from '@core/services/confirmation.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { toObservable } from '@angular/core/rxjs-interop';
import { ThemeService } from '@core/services/theme.service';
import { Actions, createEffect, ofType, OnInitEffects } from '@ngrx/effects';
import { concatLatestFrom } from '@ngrx/operators';
import { Action, Store } from '@ngrx/store';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { asyncScheduler, combineLatest, EMPTY, of } from 'rxjs';
import {
  catchError,
  distinctUntilChanged,
  filter,
  map,
  skip,
  switchMap,
  tap,
  throttleTime,
} from 'rxjs/operators';
import { workspaceBrandVariables } from '@core/util/colors/workspace-branding';
import * as actions from './workspaces.actions';
import { selectCurrentWorkspace } from './workspaces.selectors';
import { WorkspacesService } from './workspaces.service';
import { Workspace } from '@core/models/workspace';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';

@Injectable()
export class WorkspacesEffects implements OnInitEffects {
  private store = inject(Store);
  private theme = inject(ThemeService);
  private actions$ = inject<Actions<Action>>(Actions);
  private workspacesService = inject(WorkspacesService);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(SnackbarService);
  private router = inject(Router);
  private workspace = inject(WorkspaceService);

  init$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.initWorkspaces),
      concatLatestFrom(() => this.store.select(selectIsAuthenticated)),
      filter(([_, isAuth]) => isAuth),
      map(() => actions.loadWorkspaces.init())
    );
  });

  selectWorkspaceOnRouteChange$ = createEffect(() => {
    return this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => getWorkspaceSlug(event.urlAfterRedirects)),
      filter((slug): slug is string => !!slug),
      distinctUntilChanged(),
      skip(1),
      concatLatestFrom(() => this.store.select(selectCurrentWorkspace)),
      filter((pair): pair is [string, Workspace] => {
        const [slug, workspace] = pair;
        return !!workspace && workspace.slug === slug;
      }),
      map(([, workspace]) => actions.selectWorkspace({ workspace }))
    );
  });

  updatePrimaryColor$ = createEffect(
    () => {
      return combineLatest([
        this.store
          .select(selectCurrentWorkspace)
          .pipe(map((workspace) => workspace?.metaInfo?.color)),
        toObservable(this.theme.theme),
      ]).pipe(
        distinctUntilChanged(
          ([colorA, themeA], [colorB, themeB]) =>
            colorA === colorB && themeA === themeB
        ),
        tap(([color, theme]) => {
          const root = document.documentElement.style;
          const variables = workspaceBrandVariables(color, theme === 'dark');

          for (const [property, value] of Object.entries(variables)) {
            if (value) {
              root.setProperty(property, value);
            } else {
              root.removeProperty(property);
            }
          }
        })
      );
    },
    { dispatch: false }
  );

  loadWorkspaces$ = createEffect(
    ({ throttle = 800, scheduler = asyncScheduler } = {}) => {
      return this.actions$.pipe(
        ofType(actions.loadWorkspaces.init),
        throttleTime(throttle, scheduler),
        switchMap(() =>
          this.workspacesService.get().pipe(
            map((workspaces) => actions.loadWorkspaces.success({ workspaces })),
            catchError((error: HttpErrorResponse) =>
              of(actions.loadWorkspaces.fail({ error }))
            )
          )
        )
      );
    }
  );

  createWorkspace$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.createWorkspace.init),
      switchMap((action) =>
        this.workspacesService.post(action.request).pipe(
          unwrapClientReposne(),
          map((workspace) => actions.createWorkspace.success({ workspace })),
          catchError((error: HttpErrorResponse) =>
            of(actions.createWorkspace.fail({ error }))
          )
        )
      )
    );
  });

  deleteWorkspace$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.deleteWorkspace.init),
      switchMap(({ workspace }) =>
        this.workspacesService.delete(workspace).pipe(
          tap(() => {
            this.snackbar.open(
              $localize`:Confirmation shown after an action succeeds:Workspace deleted`
            );
            void this.router.navigate(['/workspaces']);
          }),
          map(() => actions.deleteWorkspace.success({ workspace })),
          catchError((error: HttpErrorResponse) =>
            of(actions.deleteWorkspace.fail({ error }))
          )
        )
      )
    );
  });

  leaveWorkspace$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.leaveWorkspace.init),
      switchMap(({ workspace }) =>
        this.confirmation.open(LEAVE_WORKSPACE_CONFIRMATION).pipe(
          switchMap((result) => {
            if (!result) return EMPTY;

            return this.workspacesService.leave(workspace).pipe(
              tap(() => {
                this.snackbar.open(`You left ${workspace.name}`);
                void this.router.navigate(['/workspaces']);
              }),
              map(() => actions.leaveWorkspace.success({ workspace })),
              catchError((error: HttpErrorResponse) =>
                of(actions.leaveWorkspace.fail({ error }))
              )
            );
          })
        )
      )
    );
  });

  editWorkspace$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.editWorkspace.init),
      switchMap((action) =>
        this.workspacesService.put(action.request).pipe(
          unwrapClientReposne(),
          map(({ workspace, previousSlug }) =>
            actions.editWorkspace.success({ workspace, previousSlug })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.editWorkspace.fail({ error }))
          )
        )
      )
    );
  });

  navigateAfterRename$ = createEffect(
    () => {
      return this.actions$.pipe(
        ofType(actions.editWorkspace.success),
        tap(({ workspace, previousSlug }) => {
          if (!previousSlug) return;

          const routeSlug = getWorkspaceSlug(this.router.url);

          if (routeSlug !== previousSlug) return;

          this.workspace.registerRename(previousSlug, workspace.slug);

          const url = replaceWorkspaceSlug(this.router.url, workspace.slug);

          void this.router.navigateByUrl(url, { replaceUrl: true });
        })
      );
    },
    { dispatch: false }
  );

  editWorkspaceFailed$ = createEffect(
    () => {
      return this.actions$.pipe(
        ofType(actions.editWorkspace.fail),
        tap(({ error }) =>
          this.snackbar.error(editWorkspaceFailureMessage(error))
        )
      );
    },
    { dispatch: false }
  );

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
              map(({ workspace, previousSlug }) =>
                actions.editWorkspace.success({ workspace, previousSlug })
              ),
              catchError((error: HttpErrorResponse) =>
                of(actions.editWorkspace.fail({ error }))
              )
            );
          })
        );
      })
    );
  });

  setWorkspacePublicPermissions$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.setWorkspacePublicPermissions),
      concatLatestFrom(() => this.store.select(selectCurrentWorkspace)),
      filter(([_, workspace]) => !!workspace?.slug),
      switchMap(([action, workspace]) => {
        const request: UpdateWorkspaceRequest = {
          /* eslint-disable-next-line @typescript-eslint/no-non-null-assertion */
          slug: workspace!.slug,
          /* eslint-disable-next-line @typescript-eslint/no-non-null-assertion */
          metaInfo: workspace!.metaInfo ?? {},
          publicPermissions: action.permissions,
        };

        return this.workspacesService.put(request).pipe(
          unwrapClientReposne(),
          map(({ workspace, previousSlug }) =>
            actions.editWorkspace.success({ workspace, previousSlug })
          ),
          catchError((error: HttpErrorResponse) =>
            of(actions.editWorkspace.fail({ error }))
          )
        );
      })
    );
  });

  isSlugUnique$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(actions.isSlugUniue.init),
      switchMap((action) =>
        this.workspacesService.isSlugUnique(action.slug).pipe(
          unwrapClientReposne(),
          map((response) => actions.isSlugUniue.success({ response })),
          catchError((error: HttpErrorResponse) =>
            of(actions.isSlugUniue.fail({ error }))
          )
        )
      )
    );
  });

  ngrxOnInitEffects(): Action {
    return actions.initWorkspaces();
  }
}

const NON_WORKSPACE_ROUTES = new Set(['auth', 'workspaces']);

const replaceWorkspaceSlug = (url: string, slug: string): string => {
  const [path, query] = url.split('?');
  const segments = path.split('/').filter(Boolean);

  if (!segments.length) return url;

  segments[0] = encodeURIComponent(slug);

  const nextPath = `/${segments.join('/')}`;

  return query ? `${nextPath}?${query}` : nextPath;
};

const editWorkspaceFailureMessage = (
  error: HttpErrorResponse | Error
): string => {
  const message =
    error instanceof HttpErrorResponse ? error.error?.message : null;

  return (
    message ??
    $localize`:Error shown after an action fails:Failed to update workspace`
  );
};

const getWorkspaceSlug = (url: string): string | null => {
  const [segment] = url.split('?')[0].split('/').filter(Boolean);

  if (!segment || NON_WORKSPACE_ROUTES.has(segment)) {
    return null;
  }

  return decodeURIComponent(segment);
};

const LEAVE_WORKSPACE_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Leave`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to leave this Workspace?`,
  title: $localize`:Title of a confirmation dialog:Leave Workspace`,
  color: 'warn',
  confirmationCheckboxLabel:
    'I understand that I will lose access to this Workspace and will need to be re-invited to rejoin.',
};

const MARK_WORKSPACE_AS_PUBLIC_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Mark as Public`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to mark this Workspace as public?`,
  title: $localize`:Title of a confirmation dialog:Mark Workspace as Public`,
  color: 'warn',
  confirmationCheckboxLabel:
    'I understand that this action will make all the content of the Workspace visible to everyone.',
};

const MARK_WORKSPACE_AS_PRIVATE_CONFIRMATION: ConfirmDialogOptions = {
  acceptLabel: $localize`:Confirms the action in a dialog:Mark as Private`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  message: $localize`:Body of a confirmation dialog:Are you sure you want to mark this Workspace as private?`,
  title: $localize`:Title of a confirmation dialog:Mark Workspace as Private`,
  color: 'warn',
  confirmationCheckboxLabel:
    'I understand that this action will make all the content of the Workspace only visible to its members.',
};
