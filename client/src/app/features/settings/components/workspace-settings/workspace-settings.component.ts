import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { toggleWorkspaceIsPublic } from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';

@Component({
  selector: 'app-workspace-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FlatButtonComponent],
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
    </div>`,
})
export class WorkspaceSettings {
  private store = inject(Store);

  isPublic = computed(() => this.workspace()?.isPublic ?? false);
  workspace = this.store.selectSignal(selectCurrentWorkspace);

  togglePublic() {
    const workspace = this.workspace();

    if (!workspace?.slug) return;

    const isPublic = !workspace.isPublic;

    this.store.dispatch(toggleWorkspaceIsPublic({ isPublic }));
  }
}
