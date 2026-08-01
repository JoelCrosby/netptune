import { Component, computed, input, output } from '@angular/core';
import { Permission } from '@core/auth/permissions';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { PermissionGroupOption } from './service-account-permissions';

export interface PermissionToggle {
  permission: Permission;
  selected: boolean;
}

@Component({
  selector: 'app-permission-grid',
  imports: [CheckboxComponent, StrokedButtonComponent],
  template: `
    <div class="mb-3 flex items-center justify-between gap-3">
      <span class="text-muted text-xs">
        <span
          i18n="
            How many permissions are selected. SELECTED is the chosen count and
            TOTAL the number available
          ">
          {{
            selectedCount() // i18n(ph="SELECTED")
          }}
          of
          {{
            totalCount() // i18n(ph="TOTAL")
          }}
          selected
        </span>
      </span>
      <div class="flex gap-2">
        <button
          app-stroked-button
          type="button"
          class="h-8 text-xs"
          (click)="selectAllRequested.emit()">
          <span i18n="Button that selects every permission"> Select all </span>
        </button>
        <button
          app-stroked-button
          type="button"
          class="h-8 text-xs"
          (click)="clearRequested.emit()">
          <span i18n="Button that deselects every permission">Clear</span>
        </button>
      </div>
    </div>

    <div
      class="border-border divide-border divide-y overflow-y-auto rounded border"
      [class]="maxHeightClass()">
      @for (group of groups(); track group.key) {
        <section>
          <header
            class="bg-foreground/3 flex items-center justify-between gap-2 px-4 py-2">
            <h4 class="text-xs font-semibold tracking-wide uppercase">
              {{ group.label }}
            </h4>
            <button
              type="button"
              class="text-primary cursor-pointer text-xs"
              (click)="toggleGroup(group)">
              {{ groupToggleLabel(group) }}
            </button>
          </header>

          <div class="divide-border grid divide-y sm:grid-cols-2 sm:divide-y-0">
            @for (permission of group.permissions; track permission.key) {
              <div class="px-4 py-2">
                <app-checkbox
                  [checked]="hasPermission(permission.key)"
                  (changed)="
                    permissionChanged.emit({
                      permission: permission.key,
                      selected: $event,
                    })
                  ">
                  <span class="text-sm">{{ permission.label }}</span>
                </app-checkbox>
              </div>
            }
          </div>
        </section>
      } @empty {
        <p class="text-muted px-4 py-3 text-sm">{{ emptyMessage() }}</p>
      }
    </div>
  `,
})
export class PermissionGridComponent {
  readonly groups = input.required<PermissionGroupOption[]>();
  readonly selected = input.required<ReadonlySet<Permission>>();
  readonly maxHeightClass = input('max-h-96');
  readonly emptyMessage = input('No permissions available.');

  readonly permissionChanged = output<PermissionToggle>();
  readonly selectAllRequested = output();
  readonly clearRequested = output();

  readonly totalCount = computed(() => {
    return this.groups().reduce((total, group) => {
      return total + group.permissions.length;
    }, 0);
  });

  readonly selectedCount = computed(() => {
    const selected = this.selected();

    return this.groups().reduce((total, group) => {
      const count = group.permissions.filter((permission) => {
        return selected.has(permission.key);
      }).length;

      return total + count;
    }, 0);
  });

  hasPermission(permission: Permission) {
    return this.selected().has(permission);
  }

  isGroupSelected(group: PermissionGroupOption) {
    const selected = this.selected();

    return group.permissions.every((permission) => {
      return selected.has(permission.key);
    });
  }

  toggleGroup(group: PermissionGroupOption) {
    const selected = !this.isGroupSelected(group);

    for (const permission of group.permissions) {
      this.permissionChanged.emit({ permission: permission.key, selected });
    }
  }

  protected groupToggleLabel(group: PermissionGroupOption): string {
    return this.isGroupSelected(group)
      ? $localize`:Button that deselects every permission in a group:Clear group`
      : $localize`:Button that selects every permission in a group:Select group`;
  }
}
