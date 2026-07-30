import { Component, computed, inject } from '@angular/core';
import {
  selectCurrentUserId,
  selectHasPermission,
} from '@core/store/auth/auth.selectors';
import { netptunePermissions } from '@app/core/auth/permissions';
import {
  deleteWorkspace,
  leaveWorkspace,
  toggleWorkspaceIsPublic,
} from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Workspace } from '@core/models/workspace';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { WorkspacePublicAccessComponent } from '../workspace-public-access/workspace-public-access.component';
import { DialogService } from '@core/services/dialog.service';
import { DeleteWorkspaceDialogComponent } from '../delete-workspace-dialog/delete-workspace-dialog.component';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-workspace-settings',
  imports: [
    FlatButtonComponent,
    StrokedButtonComponent,
    SectionHeaderComponent,
    WorkspacePublicAccessComponent,
  ],
  template: `
    <app-section-header
      i18n-heading="Section heading for workspace visibility settings"
      heading="Visibility and access" />

    <div class="flex flex-col items-start gap-4">
      @if (canUpdate()) {
        <p class="text-foreground/80 mb-2 text-sm">
          @if (workspace()?.isPublic) {
            <span i18n="Explains what public workspace visibility means">
              This workspace is currently <strong>Public</strong>. This means
              that anyone with the link can view whatever is shared below.
              However, only members of the workspace can edit it.
            </span>
          } @else {
            <span i18n="Explains what private workspace visibility means">
              This workspace is currently <strong>Private</strong>. This means
              that only members of the workspace can view and edit it.
            </span>
          }
        </p>

        <button
          app-flat-button
          color="warn"
          type="button"
          (click)="togglePublic()">
          {{ visibilityToggleLabel() }}
        </button>
      }

      <app-workspace-public-access />

      @if (canLeave()) {
        <p class="text-foreground/80 mt-4 mb-2 text-sm">
          <span i18n="Warns what happens when leaving a workspace">
            Leaving this workspace removes your access to its content. You'll
            need to be re-invited to rejoin.
          </span>
        </p>

        <button app-stroked-button color="warn" type="button" (click)="leave()">
          <span i18n="Button that removes the user from the workspace">
            Leave Workspace
          </span>
        </button>
      }
    </div>

    @if (canDelete()) {
      <div class="border-border my-8 border-b-2"></div>

      <app-section-header
        i18n-heading="Section heading above destructive workspace actions"
        heading="Danger zone"
        i18n-description="Warns about the reach of deleting a workspace"
        description="Deleting a workspace affects every member and all of its content." />

      <button
        app-flat-button
        color="warn"
        type="button"
        (click)="openDeleteDialog()">
        <span i18n="Button that opens the delete-workspace dialog">
          Delete Workspace
        </span>
      </button>
    }
  `,
})
export class WorkspaceSettings {
  private store = inject(Store);

  /** Ternaries in a template expression cannot be marked, so build the copy here. */
  readonly visibilityToggleLabel = computed(() => {
    return this.isPublic()
      ? $localize`:Button that makes a public workspace private:Mark Workspace as Private`
      : $localize`:Button that makes a private workspace public:Mark Workspace as Public`;
  });
  private dialog = inject(DialogService);

  isPublic = computed(() => this.workspace()?.isPublic ?? false);
  workspace = this.store.selectSignal(selectCurrentWorkspace);
  private currentUserId = this.store.selectSignal(selectCurrentUserId);

  canUpdate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.workspace.update)
  );
  canDelete = this.store.selectSignal(
    selectHasPermission(netptunePermissions.workspace.delete)
  );

  canLeave = computed(() => {
    const workspace = this.workspace();

    return !!workspace && workspace.ownerId !== this.currentUserId();
  });

  togglePublic() {
    const workspace = this.workspace();

    if (!workspace?.slug) return;

    const isPublic = !workspace.isPublic;

    this.store.dispatch(toggleWorkspaceIsPublic({ isPublic }));
  }

  leave() {
    const workspace = this.workspace();

    if (!workspace?.slug) return;

    this.store.dispatch(leaveWorkspace.init({ workspace }));
  }

  openDeleteDialog() {
    const workspace = this.workspace();

    if (!workspace?.slug) return;

    const dialogRef = this.dialog.open<
      boolean,
      Workspace,
      DeleteWorkspaceDialogComponent
    >(DeleteWorkspaceDialogComponent, {
      width: '600px',
      data: workspace,
      ariaLabel: `Delete ${workspace.name} workspace`,
    });

    dialogRef.closed.pipe(take(1)).subscribe((confirmed) => {
      if (confirmed) {
        this.store.dispatch(deleteWorkspace.init({ workspace }));
      }
    });
  }
}
