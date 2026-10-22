import { Component, computed, inject, input, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import {
  netptunePermissionLabels,
  PermissionGroupKey,
  permissionGroupLabels,
  PermissionMeta,
} from '@core/auth/permission-items';
import { PERMISSIONS } from '@core/auth/permissions';
import { WorkspaceAppUser } from '@core/models/appuser';
import { UserCommandsService } from '@core/services/user-commands.service';
import {
  GroupedToggleListComponent,
  ToggleListChange,
  ToggleListGroup,
  ToggleListRow,
} from '@static/components/grouped-toggle-list/grouped-toggle-list.component';
import { SegmentedOption } from '@static/components/segmented-control/segmented-control.component';

type StateFilter = 'all' | 'granted' | 'denied';

const FILTER_OPTIONS: SegmentedOption<StateFilter>[] = [
  {
    value: 'all',
    label: $localize`:Permission filter showing every permission:All`,
  },
  {
    value: 'granted',
    label: $localize`:Permission filter showing permissions the member has:Granted`,
  },
  {
    value: 'denied',
    label: $localize`:Permission filter showing permissions the member does not have:Not granted`,
  },
];

@Component({
  selector: 'app-user-permissions',
  imports: [GroupedToggleListComponent],
  host: { class: 'block' },
  template: `
    <app-grouped-toggle-list
      [groups]="groups()"
      [filterOptions]="filterOptions"
      [filtering]="isFiltering()"
      [(query)]="query"
      [(filter)]="filter"
      i18n-searchPlaceholder="Placeholder in the member permission search box"
      searchPlaceholder="Search permissions"
      i18n-filterAriaLabel="
        Accessible label for the control that filters member permissions by
        state
      "
      filterAriaLabel="Show permissions"
      i18n-emptyTitle="Empty state when no member permissions match the search"
      emptyTitle="No permissions match"
      (rowChanged)="onChanged($event)"
      (cleared)="resetFilters()" />
  `,
})
export class UserPermissionsComponent {
  private readonly userCommands = inject(UserCommandsService);

  readonly user = input<WorkspaceAppUser>();

  private readonly canUpdate = hasPermission(
    PERMISSIONS.members.updatePermissions
  );

  protected readonly filterOptions = FILTER_OPTIONS;

  protected readonly query = signal('');
  protected readonly filter = signal<StateFilter>('all');

  protected readonly isFiltering = computed(() => {
    return this.query().trim().length > 0 || this.filter() !== 'all';
  });

  protected readonly groups = computed<ToggleListGroup[]>(() => {
    const granted = new Set(this.user()?.permissions ?? []);
    const isDisabled = !this.canUpdate();
    const query = this.query().trim().toLowerCase();
    const filter = this.filter();
    const isFiltering = this.isFiltering();

    return Object.entries(netptunePermissionLabels)
      .map(([key, permissions]) => {
        const heading = permissionGroupLabels[key as PermissionGroupKey];
        const groupMatches = heading.toLowerCase().includes(query);
        const allRows = (Object.values(permissions) as PermissionMeta[]).map(
          (meta) => toRow(meta, granted.has(meta.key), isDisabled)
        );

        const rows = allRows.filter((row) => {
          const matchesQuery =
            !query || groupMatches || row.label.toLowerCase().includes(query);

          return matchesQuery && matchesFilter(row, filter);
        });

        const grantedCount = allRows.filter((row) => row.checked).length;
        const total = allRows.length;
        const shown = rows.length;

        return {
          key,
          heading,
          summary: isFiltering
            ? $localize`:How many permissions in a group match the filter:${shown}:SHOWN: of ${total}:TOTAL: shown`
            : $localize`:How many permissions in a group the member has:${grantedCount}:GRANTED: of ${total}:TOTAL: granted`,
          rows,
        };
      })
      .filter((group) => group.rows.length > 0);
  });

  protected resetFilters() {
    this.query.set('');
    this.filter.set('all');
  }

  protected onChanged(change: ToggleListChange) {
    const userId = this.user()?.id;

    if (!userId) return;

    this.userCommands.togglePermission(userId, change.key);
  }
}

function toRow(
  meta: PermissionMeta,
  granted: boolean,
  isDisabled: boolean
): ToggleListRow {
  return {
    key: meta.key,
    label: meta.label,
    checked: granted,
    ariaLabel: $localize`:Accessible label for a member permission toggle. PERMISSION is the already-localised permission name:Grant ${meta.label}:PERMISSION:`,
    icon: meta.icon,
    isDimmed: !granted,
    isDisabled,
  };
}

function matchesFilter(row: ToggleListRow, filter: StateFilter): boolean {
  switch (filter) {
    case 'all':
      return true;
    case 'granted':
      return row.checked;
    case 'denied':
      return !row.checked;
  }
}
