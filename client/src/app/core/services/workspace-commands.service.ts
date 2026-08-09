import { HttpErrorResponse } from '@angular/common/http';
import { Permission } from '@core/auth/permissions';
import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AddWorkspaceRequest } from '@core/models/requests/add-workspace-request';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';
import { UpdateWorkspaceResponse } from '@core/models/update-workspace-response';
import { Workspace } from '@core/models/workspace';
import { ConfirmationService } from '@core/services/confirmation.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { WorkspacesService } from '@core/services/workspaces-api.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, finalize, switchMap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class WorkspaceCommandsService {
  private readonly workspacesService = inject(WorkspacesService);
  private readonly list = inject(WorkspaceListService);
  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly workspace = inject(WorkspaceService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);
  private readonly router = inject(Router);

  private readonly creating = signal(false);
  private readonly editing = signal(false);

  readonly createLoading = this.creating.asReadonly();
  readonly editLoading = this.editing.asReadonly();

  create(request: AddWorkspaceRequest) {
    this.creating.set(true);

    this.workspacesService
      .post(request)
      .pipe(
        unwrapClientReposne(),
        catchError(() => EMPTY),
        finalize(() => this.creating.set(false))
      )
      .subscribe(() => this.list.reload());
  }

  edit(request: UpdateWorkspaceRequest) {
    this.editing.set(true);

    this.workspacesService
      .put(request)
      .pipe(
        unwrapClientReposne(),
        catchError((error: HttpErrorResponse | Error) => {
          this.snackbar.error(editWorkspaceFailureMessage(error));

          return EMPTY;
        }),
        finalize(() => this.editing.set(false))
      )
      .subscribe((response) => this.applyEdit(response));
  }

  delete(workspace: Workspace) {
    this.workspacesService
      .delete(workspace)
      .pipe(catchError(() => EMPTY))
      .subscribe(() => {
        this.snackbar.open(
          $localize`:Confirmation shown after an action succeeds:Workspace deleted`
        );
        this.forget(workspace);
      });
  }

  leave(workspace: Workspace) {
    this.confirmation
      .open(LEAVE_WORKSPACE_CONFIRMATION)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.workspacesService.leave(workspace);
        }),
        catchError(() => EMPTY)
      )
      .subscribe(() => {
        this.snackbar.open(`You left ${workspace.name}`);
        this.forget(workspace);
      });
  }

  setIsPublic(isPublic: boolean) {
    const workspace = this.currentWorkspace.workspace();

    if (!workspace?.slug) return;

    const confirmation = isPublic
      ? MARK_WORKSPACE_AS_PUBLIC_CONFIRMATION
      : MARK_WORKSPACE_AS_PRIVATE_CONFIRMATION;

    this.confirmation
      .open(confirmation)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.workspacesService
            .put({
              slug: workspace.slug,
              metaInfo: workspace.metaInfo ?? {},
              isPublic,
            })
            .pipe(unwrapClientReposne());
        }),
        catchError((error: HttpErrorResponse | Error) => {
          this.snackbar.error(editWorkspaceFailureMessage(error));

          return EMPTY;
        })
      )
      .subscribe((response) => this.applyEdit(response));
  }

  setPublicPermissions(permissions: Permission[]) {
    const workspace = this.currentWorkspace.workspace();

    if (!workspace?.slug) return;

    this.workspacesService
      .put({
        slug: workspace.slug,
        metaInfo: workspace.metaInfo ?? {},
        publicPermissions: permissions,
      })
      .pipe(
        unwrapClientReposne(),
        catchError((error: HttpErrorResponse | Error) => {
          this.snackbar.error(editWorkspaceFailureMessage(error));

          return EMPTY;
        })
      )
      .subscribe((response) => this.applyEdit(response));
  }

  private applyEdit({ workspace, previousSlug }: UpdateWorkspaceResponse) {
    this.currentWorkspace.apply(workspace);
    this.list.reload();

    if (!previousSlug) return;

    const routeSlug = getWorkspaceSlug(this.router.url);

    if (routeSlug !== previousSlug) return;

    // The open URL still names the workspace by its old slug, so it has to be
    // rewritten in place or the next navigation 404s.
    this.workspace.registerRename(previousSlug, workspace.slug);

    void this.router.navigateByUrl(
      replaceWorkspaceSlug(this.router.url, workspace.slug),
      { replaceUrl: true }
    );
  }

  private forget(workspace: Workspace) {
    this.currentWorkspace.clearIfCurrent(workspace);
    this.list.reload();

    void this.router.navigate(['/workspaces']);
  }
}

const NON_WORKSPACE_ROUTES = new Set(['auth', 'workspaces']);

export const getWorkspaceSlug = (url: string): string | null => {
  const [segment] = url.split('?')[0].split('/').filter(Boolean);

  if (!segment || NON_WORKSPACE_ROUTES.has(segment)) {
    return null;
  }

  return decodeURIComponent(segment);
};

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
