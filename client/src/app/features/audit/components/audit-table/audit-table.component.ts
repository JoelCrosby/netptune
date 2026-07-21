import { Component, computed, inject, viewChild } from '@angular/core';
import { Params } from '@angular/router';
import { AuditStore } from '@audit/audit-state.service';
import { ActivityType } from '@core/models/view-models/activity-view-model';
import { AuditLogViewModel } from '@core/models/view-models/audit-log-view-model';
import { DialogService } from '@core/services/dialog.service';
import { LucideExternalLink } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { ActivityTypePipe } from '@static/pipes/activity-type.pipe';
import { EntityTypePipe } from '@static/pipes/entity-type.pipe';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { AuditLogDetailDialogComponent } from '../../dialogs/audit-log-detail-dialog.component';

@Component({
  selector: 'app-audit-table',
  imports: [
    ActivityTypePipe,
    DatatableCellTemplateDirective,
    DatatableComponent,
    EntityTypePipe,
    IconButtonComponent,
    LucideExternalLink,
    PrettyDatePipe,
  ],
  template: `
    <app-datatable
      containerClass="h-[calc(100vh-42rem)] overflow-auto"
      tableClass="min-w-180 table-fixed"
      emptyMessage="No audit events found."
      itemLabel="events"
      [data]="data"
      [stickyHeader]="true">
      <ng-template appDatatableCell="occurredAt" let-row>
        <span class="text-foreground/70 font-mono text-xs whitespace-nowrap">
          {{ row.occurredAt | prettyDate }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="userDisplayName" let-row>
        <span class="font-medium">{{ row.userDisplayName }}</span>
      </ng-template>

      <ng-template appDatatableCell="type" let-row>
        <span
          [class]="
            'rounded px-2 py-0.5 text-xs font-medium ' + pillClass(row.type)
          ">
          {{ row.type | activityType }}
        </span>
      </ng-template>

      <ng-template appDatatableCell="entityType" let-row>
        <span class="text-foreground/80">
          {{ row.entityType | entityType }}
          @if (row.entityId) {
            <span class="text-foreground/50"> #{{ row.entityId }}</span>
          }
        </span>
      </ng-template>

      <ng-template appDatatableCell="context" let-row>
        <span class="text-foreground/70 text-sm">{{ row.summary }}</span>
      </ng-template>

      <ng-template appDatatableCell="details" let-row>
        <div class="flex justify-end">
          <button
            app-icon-button
            type="button"
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
  private readonly state = inject(AuditStore);
  private readonly dialog = inject(DialogService);
  private readonly datatable = viewChild.required(
    DatatableComponent<AuditLogViewModel>
  );

  private readonly resourceParams = computed<Params>(() => {
    const filter = this.state.filter();

    return {
      userId: filter.userId,
      entityType: filter.entityType,
      activityType: filter.activityType,
      from: filter.from,
      to: filter.to,
    };
  });

  protected readonly data: DatatableDataSource<AuditLogViewModel> = {
    key: 'audit-log',
    columns: [
      { id: 'occurredAt', header: 'Timestamp', widthClass: 'w-64' },
      { id: 'userDisplayName', header: 'Actor', widthClass: 'w-48' },
      { id: 'type', header: 'Action', widthClass: 'w-48' },
      { id: 'entityType', header: 'Entity', widthClass: 'w-40' },
      { id: 'context', header: 'Context' },
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
      case ActivityType.loginSuccess:
        return 'bg-green-500/10 text-green-600 dark:text-green-400';
      case ActivityType.delete:
      case ActivityType.remove:
      case ActivityType.removeTag:
      case ActivityType.removeRelation:
      case ActivityType.loginFailed:
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
