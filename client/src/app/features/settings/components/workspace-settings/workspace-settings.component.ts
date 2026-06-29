import { Component, computed, inject } from '@angular/core';
import { selectCurrentUserId } from '@core/store/auth/auth.selectors';
import {
  leaveWorkspace,
  toggleWorkspaceIsPublic,
} from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-workspace-settings',
  imports: [FlatButtonComponent, StrokedButtonComponent],
  template: `<h3 class="font-overpass text-[1.4rem] font-normal">Workspace</h3>

    <div class="mt-4 flex flex-col items-start gap-4">
      <p class="text-foreground/80 mb-2 text-sm">
        @if (workspace()?.isPublic) {
          This workspace is currently <strong>Public</strong>. This means that
          anyone with the link to this workspace can view it. However, only
          members of the workspace can edit it.
        } @else {
          This workspace is currently <strong>Private</strong>. This means that
          only members of the workspace can view and edit it.
        }
      </p>

      <button app-flat-button color="warn" (click)="togglePublic()">
        {{
          isPublic() ? 'Mark Workspace as Private' : 'Mark Workspace as Public'
        }}
      </button>

      @if (canLeave()) {
        <p class="text-foreground/80 mt-4 mb-2 text-sm">
          Leaving this workspace removes your access to its content. You'll need
          to be re-invited to rejoin.
        </p>

        <button app-stroked-button color="warn" (click)="leave()">
          Leave Workspace
        </button>
      }
    </div>`,
})
export class WorkspaceSettings {
  private store = inject(Store);

  isPublic = computed(() => this.workspace()?.isPublic ?? false);
  workspace = this.store.selectSignal(selectCurrentWorkspace);
  private currentUserId = this.store.selectSignal(selectCurrentUserId);

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
}
