import { Component, computed, inject } from '@angular/core';
import {
  netptunePermissions,
  Permission,
  publicReadablePermissions,
} from '@core/auth/permissions';
import { permissionLabel } from '@settings/components/service-accounts/service-account-permissions';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { setWorkspacePublicPermissions } from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';

@Component({
  selector: 'app-workspace-public-access',
  imports: [CheckboxComponent, StrokedButtonComponent],
  host: {
    class: 'block w-full',
  },
  template: `
    @if (editable()) {
      <h4 class="mb-1 text-sm font-medium">Public access</h4>
      <p class="text-muted mb-3 text-xs">
        Choose what visitors without an account can see. Everything else —
        members, comments, activity, files and workspace settings — stays
        private to members regardless.
      </p>

      <div class="mb-3 flex items-center justify-between gap-3">
        <span class="text-muted text-xs">
          {{ selected().size }} of {{ options.length }} shared
        </span>
        <div class="flex gap-2">
          <button
            app-stroked-button
            type="button"
            class="h-8 text-xs"
            (click)="shareAll()">
            Share all
          </button>
          <button
            app-stroked-button
            type="button"
            class="h-8 text-xs"
            (click)="shareNone()">
            Share none
          </button>
        </div>
      </div>

      <div class="border-border divide-border divide-y rounded border">
        @for (option of options; track option.key) {
          <div class="px-4 py-2">
            <app-checkbox
              [checked]="isShared(option.key)"
              (changed)="setShared(option.key, $event)">
              <span class="text-sm">{{ option.label }}</span>
            </app-checkbox>
          </div>
        }
      </div>

      @if (selected().size === 0) {
        <p class="text-muted mt-2 text-xs">
          Visitors can open the workspace but will not see any of its content.
        </p>
      }
    }
  `,
})
export class WorkspacePublicAccessComponent {
  private store = inject(Store);

  private workspace = this.store.selectSignal(selectCurrentWorkspace);
  private canUpdate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.workspace.update)
  );

  readonly options = publicReadablePermissions.map((permission) => {
    return { key: permission, label: permissionLabel(permission) };
  });

  readonly editable = computed(() => {
    return this.workspace()?.isPublic === true && this.canUpdate();
  });

  readonly selected = computed(() => {
    const permissions =
      this.workspace()?.publicPermissions ?? publicReadablePermissions;

    return new Set(permissions);
  });

  isShared(permission: Permission) {
    return this.selected().has(permission);
  }

  setShared(permission: Permission, shared: boolean) {
    const permissions = new Set(this.selected());

    if (shared) {
      permissions.add(permission);
    } else {
      permissions.delete(permission);
    }

    this.save([...permissions]);
  }

  shareAll() {
    this.save([...publicReadablePermissions]);
  }

  shareNone() {
    this.save([]);
  }

  private save(permissions: Permission[]) {
    this.store.dispatch(setWorkspacePublicPermissions({ permissions }));
  }
}
