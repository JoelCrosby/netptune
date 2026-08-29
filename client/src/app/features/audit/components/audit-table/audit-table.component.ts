import { Component, computed, inject, viewChild } from '@angular/core';
import { Params } from '@angular/router';
import { AuditFilterService } from '@audit/audit-filter.service';
import { auditFilterParams } from '@core/resources/audit.resource';
import { ActivityType } from '@core/models/view-models/activity-view-model';
import { AuditLogViewModel } from '@core/models/view-models/audit-log-view-model';
import { DialogService } from '@core/services/dialog.service';
import { LucideExternalLink } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { ActivityTypePipe } from '@static/pipes/activity-type.pipe';
import { EntityTypePipe } from '@static/pipes/entity-type.pipe';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { AuditLogDetailDialogComponent } from '../../dialogs/audit-log-detail-dialog.component';

@Component({
  selector: 'app-audit-table',
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  imports: [
    ActivityTypePipe,
    AvatarComponent,
    BadgeComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    EntityTypePipe,
    IconButtonComponent,
    LucideExternalLink,
    PrettyDatePipe,
    TooltipDirective,
  ],
  template: `
    <app-datatable
      autoFill
      i18n-errorMessage="Shown when the audit log fails to load"
      errorMessage="Audit events could not be loaded."
      stickyHeader
      headerClass="bg-card-header text-muted uppercase"
      tableClass="min-w-180 table-fixed"
      i18n-emptyMessage="Empty state for the audit log"
      emptyMessage="No audit events found."
      i18n-itemLabel="
        Plural noun for audit entries, used in the selection summary
      "
      itemLabel="events"
      [data]="data"
      [stickyHeader]="true">
      <ng-template appDatatableCell="occurredAt" let-row>
        <span class="text-foreground/70 font-mono text-xs whitespace-nowrap">
          {{ row.occurredAt | prettyDate }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="userDisplayName" let-row>
        <div class="flex min-w-0 items-center gap-2">
          <app-avatar
            class="shrink-0"
            size="sm"
            [name]="row.userDisplayName"
            [imageUrl]="row.userPictureUrl" />
          <span class="min-w-0 truncate">
            <span class="font-medium">{{ row.userDisplayName }}</span>
            @if (row.agent) {
              <span class="text-muted ml-1 text-xs">
                <span
                  i18n="
                    Precedes the assistant that made a change on the user's
                    behalf
                  "
                  >via</span
                >
                {{ row.agent }}
              </span>
            }
          </span>
        </div>
      </ng-template>

      <ng-template appDatatableCell="type" let-row>
        <app-badge shape="rounded" [class]="pillClass(row.type)">
          {{ row.type | activityType }}
        </app-badge>
      </ng-template>

      <ng-template appDatatableCell="entityType" let-row>
        <span class="text-foreground/80 block truncate">
          {{ row.entityType | entityType }}
          @if (row.entityId) {
            <span class="text-foreground/50">#{{ row.entityId }}</span>
          }
        </span>
      </ng-template>

      <ng-template appDatatableCell="context" let-row>
        <span
          class="text-foreground/70 block truncate text-sm"
          [appTooltip]="row.summary">
          {{ row.summary }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="details" let-row>
        <div class="flex justify-end">
          <button
            app-icon-button
            type="button"
            i18n-aria-label="
              Accessible label for the button that opens an audit entry
            "
            aria-label="View full audit log details"
            (click)="openDetails(row)">
            <svg lucideExternalLink class="h-4 w-4"></svg>
          </button>
        </div>
      </ng-template>
    </app-datatable>
  `,
})
export class AuditTableComponent {
  private readonly filters = inject(AuditFilterService);
  private readonly dialog = inject(DialogService);
  private readonly datatable = viewChild.required(
    DatatableComponent<AuditLogViewModel>
  );

  private readonly resourceParams = computed<Params>(() => {
    return auditFilterParams(this.filters.filter());
  });

  protected readonly data: DatatableDataSource<AuditLogViewModel> = {
    key: 'audit-log',
    columns: [
      {
        id: 'occurredAt',
        header: 'Timestamp',
        widthClass: 'w-64',
        cellClass: 'whitespace-nowrap',
      },
      {
        id: 'userDisplayName',
        header: 'Actor',
        widthClass: 'w-56',
        cellClass: 'overflow-hidden',
      },
      {
        id: 'type',
        header: 'Action',
        widthClass: 'w-48',
        cellClass: 'whitespace-nowrap',
      },
      {
        id: 'entityType',
        header: 'Entity',
        widthClass: 'w-40',
        cellClass: 'overflow-hidden',
      },
      { id: 'context', header: 'Context', cellClass: 'overflow-hidden' },
      {
        id: 'details',
        header: '',
        align: 'end',
        ariaLabel: 'Details',
        cellClass: 'px-2 py-0',
        widthClass: 'w-14',
      },
    ],
    resource: {
      url: 'api/audit',
      params: this.resourceParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, row: AuditLogViewModel) => row.id,
  };

  goToFirstPage() {
    this.datatable().goToPage(1);
  }

  protected openDetails(row: AuditLogViewModel) {
    this.dialog.open(AuditLogDetailDialogComponent, {
      ariaLabel: 'Audit log details',
      data: { id: row.id },
      width: AuditLogDetailDialogComponent.width,
    });
  }

  protected pillClass(type: ActivityType): string {
    switch (type) {
      case ActivityType.create:
      case ActivityType.addTag:
      case ActivityType.addRelation:
      case ActivityType.addComment:
      case ActivityType.loginSuccess:
      case ActivityType.importCompleted:
      case ActivityType.exportCompleted:
        return 'bg-green-500/10 text-green-600 dark:text-green-400';
      case ActivityType.delete:
      case ActivityType.remove:
      case ActivityType.removeTag:
      case ActivityType.removeRelation:
      case ActivityType.removeComment:
      case ActivityType.loginFailed:
      case ActivityType.importFailed:
      case ActivityType.exportFailed:
        return 'bg-red-500/10 text-red-600 dark:text-red-400';
      case ActivityType.assign:
      case ActivityType.unassign:
        return 'bg-violet-500/10 text-violet-600 dark:text-violet-400';
      case ActivityType.invite:
      case ActivityType.permissionChanged:
      case ActivityType.roleChanged:
        return 'bg-sky-500/10 text-sky-600 dark:text-sky-400';
      case ActivityType.exportRequested:
        return 'bg-indigo-500/10 text-indigo-600 dark:text-indigo-400';
      default:
        return 'bg-amber-500/10 text-amber-600 dark:text-amber-400';
    }
  }
}
