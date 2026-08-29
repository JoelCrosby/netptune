import { Component, computed, inject, output } from '@angular/core';
import { AuditFilterService } from '@audit/audit-filter.service';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { EntityType } from '@core/models/entity-type';
import { ActivityType } from '@core/models/view-models/activity-view-model';
import { AuditLogFilter } from '@core/models/view-models/audit-log-view-model';
import { userResource } from '@core/resources/user.resource';
import { AuditService } from '@core/services/audit.service';
import { activityTypeToString } from '@core/transforms/activity-type';
import { entityTypeToString } from '@core/transforms/entity-type';
import { downloadFile } from '@core/util/download-helper';
import { LucideDownload, LucideShapes, LucideZap } from '@lucide/angular';
import {
  AvatarFilterComponent,
  AvatarFilterOption,
} from '@static/components/avatar-filter/avatar-filter.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { FilterSeparatorComponent } from '@static/components/filter-separator/filter-separator.component';
import {
  SelectFilterComponent,
  SelectFilterOption,
} from '@static/components/select-filter/select-filter.component';
import { AuditDateRangeComponent } from './audit-date-range.component';

@Component({
  selector: 'app-audit-filters',
  imports: [
    AuditDateRangeComponent,
    AvatarFilterComponent,
    FilterSeparatorComponent,
    FlatButtonComponent,
    LucideDownload,
    SelectFilterComponent,
  ],
  host: { class: 'block w-full' },
  template: `
    <div class="flex flex-row flex-wrap items-center gap-3">
      @if (users.canRead()) {
        <app-avatar-filter
          i18n-emptyLabel="Shown when there are no members to filter by"
          emptyLabel="No members"
          [options]="userOptions()"
          (optionClicked)="toggleUser($event)" />
        <app-filter-separator />
      }

      <app-select-filter
        i18n-label="Label on the control that filters the audit log by entity"
        label="Filter by Entity"
        i18n-emptyLabel="Filter option including every entity type"
        emptyLabel="All entities"
        [icon]="entityIcon"
        [options]="entityOptions"
        [value]="filter().entityType ?? null"
        (changed)="apply({ entityType: $event ?? undefined })" />

      <app-filter-separator />

      <app-select-filter
        i18n-label="Label on the control that filters the audit log by action"
        label="Filter by Action"
        i18n-emptyLabel="Filter option including every action"
        emptyLabel="All actions"
        [icon]="actionIcon"
        [options]="activityOptions"
        [value]="filter().activityType ?? null"
        (changed)="apply({ activityType: $event ?? undefined })" />

      <app-filter-separator />

      <app-audit-date-range
        [from]="filter().from ?? ''"
        [to]="filter().to ?? ''"
        (fromChanged)="apply({ from: $event || undefined })"
        (toChanged)="apply({ to: $event || undefined })" />

      <div class="ml-auto flex flex-wrap items-center gap-2">
        @if (filters.hasFilters()) {
          <button
            type="button"
            class="text-muted-foreground hover:bg-muted hover:text-foreground cursor-pointer rounded px-3 py-2 text-sm font-medium transition-colors"
            (click)="clear()">
            <span i18n="Button that clears every active filter">
              Clear filters
            </span>
          </button>
        }

        @if (canExport()) {
          <button
            app-flat-button
            type="button"
            class="gap-2"
            (click)="onExport()">
            <svg lucideDownload class="h-4 w-4"></svg>
            <span i18n="Button that downloads the audit log as CSV">
              Export CSV
            </span>
          </button>
        }
      </div>
    </div>
  `,
})
export class AuditFiltersComponent {
  protected readonly filters = inject(AuditFilterService);
  private readonly auditService = inject(AuditService);

  protected readonly canExport = hasPermission(PERMISSIONS.audit.export);

  protected readonly users = userResource();

  protected readonly filter = this.filters.filter;

  readonly filterChange = output();

  protected readonly entityIcon = LucideShapes;
  protected readonly actionIcon = LucideZap;

  protected readonly entityOptions = toOptions(EntityType, entityTypeToString);
  protected readonly activityOptions = toOptions(
    ActivityType,
    activityTypeToString
  );

  protected readonly userOptions = computed<AvatarFilterOption[]>(() => {
    const selected = this.filter().userId;

    return (this.users.value()?.payload?.items ?? []).map((user) => ({
      id: user.id,
      displayName: user.displayName,
      pictureUrl: user.pictureUrl,
      isServiceAccount: user.isServiceAccount,
      selected: user.id === selected,
    }));
  });

  protected toggleUser(option: AvatarFilterOption) {
    const isSelected = this.filter().userId === option.id;

    this.apply({ userId: isSelected ? undefined : option.id });
  }

  protected apply(patch: AuditLogFilter) {
    this.filters.update(patch);
    this.filterChange.emit();
  }

  protected clear() {
    this.filters.reset();
    this.filterChange.emit();
  }

  protected onExport() {
    this.auditService.exportAuditLog(this.filter()).subscribe((response) => {
      const cd = response.headers.get('content-disposition') ?? '';
      const blob = response.body;

      if (!blob) return;

      const filename =
        cd.match(/filename="?([^"]+)"?/)?.[1] ?? 'netptune-audit-export.csv';

      downloadFile(blob, filename);
    });
  }
}

function toOptions<T extends number>(
  values: Record<string, string | number>,
  toLabel: (value: T) => string
): SelectFilterOption<T>[] {
  const options = Object.values(values)
    .filter((value): value is number => typeof value === 'number')
    .map((value) => ({ value: value as T, label: toLabel(value as T) }));

  return options.sort((left, right) => left.label.localeCompare(right.label));
}
