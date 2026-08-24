import { inject, Service, signal } from '@angular/core';
import { BrandingImage, BrandingTarget } from '@core/models/branding';
import { BrandingService } from '@core/services/branding.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { getErrorMessage } from '@core/util/error-message';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, finalize } from 'rxjs';

@Service()
export class BrandingCommandsService {
  private readonly branding = inject(BrandingService);
  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly workspaceList = inject(WorkspaceListService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly snackbar = inject(SnackbarService);

  private readonly uploading = signal(false);

  readonly isUploading = this.uploading.asReadonly();

  upload(
    target: BrandingTarget,
    file: File,
    onChange?: (fileId: string | null) => void
  ) {
    this.uploading.set(true);

    this.branding
      .upload(target, file)
      .pipe(
        unwrapClientResponse(),
        catchError((error: unknown) => {
          this.snackbar.error(getErrorMessage(error));

          return EMPTY;
        }),
        finalize(() => this.uploading.set(false))
      )
      .subscribe((image: BrandingImage) => {
        this.applyChange(target, image.fileId);
        onChange?.(image.fileId);
      });
  }

  remove(target: BrandingTarget, onChange?: (fileId: string | null) => void) {
    this.branding
      .remove(target)
      .pipe(
        catchError((error: unknown) => {
          this.snackbar.error(getErrorMessage(error));

          return EMPTY;
        })
      )
      .subscribe(() => {
        this.applyChange(target, null);
        onChange?.(null);
      });
  }

  private applyChange(target: BrandingTarget, fileId: string | null) {
    if (target.kind === 'workspaceLogo') {
      this.applyWorkspaceLogo(fileId);

      return;
    }

    if (target.kind === 'projectLogo') {
      this.workspaceRefresh.refresh(['projects']);

      return;
    }

    this.workspaceRefresh.refresh(['boards', 'boardGroups']);
  }

  private applyWorkspaceLogo(fileId: string | null) {
    const workspace = this.currentWorkspace.workspace();

    if (!workspace) return;

    this.currentWorkspace.apply({
      ...workspace,
      metaInfo: { ...workspace.metaInfo, logoFileId: fileId },
    });

    this.workspaceList.reload();
  }
}
