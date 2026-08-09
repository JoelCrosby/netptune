import { Component, computed, inject } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { hasPermission } from '@core/auth/has-permission';
import { WorkspaceCommandsService } from '@core/services/workspace-commands.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { PERMISSONS } from '@app/core/auth/permissions';
import { Workspace } from '@core/models/workspace';
import {
  LucideGlobe,
  LucideLogOut,
  LucideTriangleAlert,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { WorkspacePublicAccessComponent } from '../workspace-public-access/workspace-public-access.component';
import { DialogService } from '@core/services/dialog.service';
import { DeleteWorkspaceDialogComponent } from '../delete-workspace-dialog/delete-workspace-dialog.component';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-workspace-settings',
  imports: [
    FlatButtonComponent,
    StrokedButtonComponent,
    IconTileComponent,
    WorkspacePublicAccessComponent,
  ],
  host: { class: 'block' },
  template: `
    <div class="flex flex-col gap-6">
      <section
        class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
        <header class="border-border border-b px-6 py-5">
          <div class="flex min-w-0 items-center gap-3">
            <app-icon-tile [icon]="visibilityIcon" />

            <div class="min-w-0">
              <h2
                class="font-overpass text-base font-semibold"
                i18n="Section heading for workspace visibility settings">
                Visibility and access
              </h2>
              <p class="text-muted mt-1 text-sm">
                @if (workspace()?.isPublic) {
                  <span i18n="Explains what public workspace visibility means">
                    Anyone with the link can view whatever is shared below, but
                    only members can edit it.
                  </span>
                } @else {
                  <span i18n="Explains what private workspace visibility means">
                    Only members of this workspace can view and edit it.
                  </span>
                }
              </p>
            </div>
          </div>
        </header>

        @if (canUpdate()) {
          <div class="border-border border-b px-6 py-4">
            <button
              app-stroked-button
              color="warn"
              type="button"
              (click)="togglePublic()">
              {{ visibilityToggleLabel() }}
            </button>
          </div>
        }

        @if (showPublicAccess()) {
          <div class="px-6 py-5">
            <app-workspace-public-access />
          </div>
        }
      </section>

      @if (canLeave()) {
        <section
          class="border-border bg-card flex flex-wrap items-center justify-between gap-x-4 gap-y-3 rounded-lg border px-6 py-5 shadow-sm">
          <div class="flex min-w-0 items-center gap-3">
            <app-icon-tile [icon]="leaveIcon" />

            <div class="min-w-0">
              <h2
                class="font-overpass text-base font-semibold"
                i18n="Heading of the leave workspace card">
                Leave workspace
              </h2>
              <p
                class="text-muted mt-1 text-sm"
                i18n="Warns what happens when leaving a workspace">
                You lose access to this workspace's content and need to be
                re-invited to rejoin.
              </p>
            </div>
          </div>

          <button
            app-stroked-button
            color="warn"
            type="button"
            class="shrink-0"
            (click)="leave()">
            <span i18n="Button that removes the user from the workspace">
              Leave Workspace
            </span>
          </button>
        </section>
      }

      @if (canDelete()) {
        <section
          class="border-warn/40 bg-card flex flex-wrap items-center justify-between gap-x-4 gap-y-3 rounded-lg border px-6 py-5 shadow-sm">
          <div class="flex min-w-0 items-center gap-3">
            <app-icon-tile [icon]="dangerIcon" class="bg-warn/10 text-warn" />

            <div class="min-w-0">
              <h2
                class="font-overpass text-warn text-base font-semibold"
                i18n="Section heading above destructive workspace actions">
                Danger zone
              </h2>
              <p
                class="text-muted mt-1 text-sm"
                i18n="Warns about the reach of deleting a workspace">
                Deleting a workspace affects every member and all of its
                content.
              </p>
            </div>
          </div>

          <button
            app-flat-button
            color="warn"
            type="button"
            class="shrink-0"
            (click)="openDeleteDialog()">
            <span i18n="Button that opens the delete-workspace dialog">
              Delete Workspace
            </span>
          </button>
        </section>
      }
    </div>
  `,
})
export class WorkspaceSettings {
  private workspaceCommands = inject(WorkspaceCommandsService);

  protected readonly visibilityIcon = LucideGlobe;
  protected readonly leaveIcon = LucideLogOut;
  protected readonly dangerIcon = LucideTriangleAlert;

  /** Ternaries in a template expression cannot be marked, so build the copy here. */
  readonly visibilityToggleLabel = computed(() => {
    return this.isPublic()
      ? $localize`:Button that makes a public workspace private:Mark Workspace as Private`
      : $localize`:Button that makes a private workspace public:Mark Workspace as Public`;
  });
  private dialog = inject(DialogService);

  isPublic = computed(() => this.workspace()?.isPublic ?? false);
  workspace = inject(CurrentWorkspaceService).workspace;
  private currentUserId = inject(SessionService).currentUserId;

  canUpdate = hasPermission(PERMISSONS.workspace.update);
  canDelete = hasPermission(PERMISSONS.workspace.delete);

  /** Mirrors the condition the public access panel renders under, so the card
   * does not leave an empty padded block when it has nothing to show. */
  showPublicAccess = computed(() => this.isPublic() && this.canUpdate());

  canLeave = computed(() => {
    const workspace = this.workspace();

    return !!workspace && workspace.ownerId !== this.currentUserId();
  });

  togglePublic() {
    const workspace = this.workspace();

    if (!workspace?.slug) return;

    const isPublic = !workspace.isPublic;

    this.workspaceCommands.setIsPublic(isPublic);
  }

  leave() {
    const workspace = this.workspace();

    if (!workspace?.slug) return;

    this.workspaceCommands.leave(workspace);
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
        this.workspaceCommands.delete(workspace);
      }
    });
  }
}
