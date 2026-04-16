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
import { selectHasPermission } from '@app/core/auth/store/auth.selectors';
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
    class:
      'block w-full bg-board-group overflow-auto  custom-scroll rounded border-border/60 border',
  },
  template: `
    <div class="flex w-full flex-col">
      @for (group of groups(); track group.heading) {
        <div class="w-full">
          <h4
            class="border-border text-foreground/80 bg-background sticky top-0 mb-2 border-b px-4 pt-8 pb-4 tracking-wide capitalize">
            {{ group.heading }}
          </h4>
          <div class="flex w-full flex-col">
            @for (item of group.items; track item.key) {
              <div
                class="border-border flex h-14 w-full items-center gap-3 border-b px-4"
                [class.opacity-40]="!item.granted">
                <svg [lucideIcon]="item.icon" class="h-6 w-6 shrink-0"></svg>
                <span class="flex-1 text-sm">{{ item.label }}</span>

                <app-checkbox
                  [checked]="item.granted"
                  [disabled]="enabled()"
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
