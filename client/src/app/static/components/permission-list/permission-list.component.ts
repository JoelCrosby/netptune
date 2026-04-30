import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { selectUserDetail } from '@app/core/store/users/users.selectors';
import {
  netptunePermissionLabels,
  PermissionMeta,
} from '@core/auth/permission-items';
import { LucideDynamicIcon } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { CheckboxComponent } from '../checkbox/checkbox.component';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { toggleUserPermission } from '@app/core/store/users/users.actions';

interface PermissionItem extends PermissionMeta {
  granted: boolean;
}

interface PermissionGroup {
  heading: string;
  items: PermissionItem[];
}

@Component({
  selector: 'app-permission-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideDynamicIcon, CheckboxComponent],
  host: {
    class: 'block w-full rounded ',
  },
  template: `
    <div class="flex w-full flex-col gap-6">
      @for (group of groups(); track group.heading) {
        <div class="border-border/60 w-full rounded border">
          <h4
            class="border-border/60 text-foreground/80 bg-background sticky top-0 z-20 rounded-t border-b px-4 py-2 tracking-wide capitalize">
            {{ group.heading }}
          </h4>
          <div class="flex w-full flex-col">
            @for (item of group.items; track item.key; let last = $last) {
              <div
                class="bg-board-group border-border hover:bg-hover flex h-10 w-full cursor-pointer items-center gap-3 border-b px-4"
                [class.border-0!]="last">
                <svg
                  [lucideIcon]="item.icon"
                  class="h-4 w-4 shrink-0"
                  [class.opacity-40]="!item.granted"></svg>
                <span
                  class="flex-1 text-sm"
                  [class.opacity-40]="!item.granted"
                  >{{ item.label }}</span
                >

                <app-checkbox
                  #check
                  [checked]="item.granted"
                  [disabled]="!enabled()"
                  (changed)="onChanged(item)" />
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class PermissionListComponent {
  readonly store = inject(Store);

  user = this.store.selectSignal(selectUserDetail);
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

    this.store.dispatch(
      toggleUserPermission({
        permission: permission.key,
        userId,
      })
    );
  }
}
