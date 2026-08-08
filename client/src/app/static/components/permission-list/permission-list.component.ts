import { Component, computed, inject, input } from '@angular/core';
import {
  netptunePermissionLabels,
  PermissionMeta,
} from '@core/auth/permission-items';
import { LucideDynamicIcon } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { CheckboxComponent } from '../checkbox/checkbox.component';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { UserCommandsService } from '@core/services/user-commands.service';
import { WorkspaceAppUser } from '@core/models/appuser';

interface PermissionItem extends PermissionMeta {
  granted: boolean;
}

interface PermissionGroup {
  heading: string;
  items: PermissionItem[];
}

@Component({
  selector: 'app-permission-list',
  imports: [LucideDynamicIcon, CheckboxComponent],
  host: { class: 'block w-full' },
  template: `
    @for (group of groups(); track group.heading) {
      <section class="border-border border-t first:border-t-0">
        <h4
          class="border-border bg-card-header text-muted border-b px-6 py-2 text-xs font-medium tracking-wide uppercase">
          {{ group.heading }}
        </h4>

        @for (item of group.items; track item.key) {
          <div
            class="border-border/60 hover:bg-hover flex w-full items-center gap-3 border-b px-6 py-3 last:border-b-0">
            <svg
              [lucideIcon]="item.icon"
              class="h-4 w-4 shrink-0"
              [class.opacity-40]="!item.granted"></svg>
            <span class="flex-1 text-sm" [class.opacity-40]="!item.granted">
              {{ item.label }}
            </span>

            <app-checkbox
              [checked]="item.granted"
              [disabled]="!enabled()"
              (changed)="onChanged(item)" />
          </div>
        }
      </section>
    }
  `,
})
export class PermissionListComponent {
  readonly store = inject(Store);
  private readonly userCommands = inject(UserCommandsService);

  readonly user = input<WorkspaceAppUser>();
  permissions = computed(() => this.user()?.permissions || []);

  enabled = this.store.selectSignal(
    selectHasPermission(netptunePermissions.members.updatePermissions)
  );

  readonly groups = computed<PermissionGroup[]>(() => {
    const permSet = new Set(this.permissions());
    const groups: PermissionGroup[] = [];

    for (const [groupKey, groupValue] of Object.entries(
      netptunePermissionLabels
    )) {
      const items = Object.values(
        groupValue as Record<string, PermissionMeta>
      ).map((meta) => ({ ...meta, granted: permSet.has(meta.key) }));

      groups.push({
        heading: groupKey.replace(/([A-Z])/g, ' $1').trim(),
        items,
      });
    }

    return groups;
  });

  onChanged(permission: PermissionItem) {
    const userId = this.user()?.id;

    if (!userId) return;

    this.userCommands.togglePermission(userId, permission.key);
  }
}
