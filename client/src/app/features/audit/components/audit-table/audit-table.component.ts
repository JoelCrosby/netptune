import { Component, inject } from '@angular/core';
import { AuditStore } from '@audit/audit-state.service';
import { ActivityType } from '@core/models/view-models/activity-view-model';
import { ActivityTypePipe } from '@static/pipes/activity-type.pipe';
import { EntityTypePipe } from '@static/pipes/entity-type.pipe';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import {
  TableComponent,
  TableEmptyCellDirective,
  TableHeaderRowDirective,
  TableHeadDirective,
  TablePaginationComponent,
  TableRowDirective,
} from '@static/components/table/table.component';

@Component({
  selector: 'app-audit-table',
  imports: [
    ActivityTypePipe,
    EntityTypePipe,
    PrettyDatePipe,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TablePaginationComponent,
    TableRowDirective,
  ],
  template: `
    @if (state.loading()) {
      <p class="text-foreground/50 py-12 text-center text-sm">Loading…</p>
    } @else if (state.error()) {
      <p class="py-12 text-center text-sm text-red-500">
        Failed to load audit log.
      </p>
    } @else {
      <app-table containerClass="h-[calc(100vh-42rem)] overflow-auto">
        <thead appTableHead [sticky]="true">
          <tr appTableHeaderRow>
            <th class="px-4 py-3">Timestamp</th>
            <th class="px-4 py-3">Actor</th>
            <th class="px-4 py-3">Action</th>
            <th class="px-4 py-3">Entity</th>
            <th class="px-4 py-3">Context</th>
          </tr>
        </thead>
        <tbody>
          @for (row of state.items(); track row.id) {
            <tr appTableRow>
              <td
                class="text-foreground/70 px-4 py-2.5 font-mono text-xs whitespace-nowrap">
                {{ row.occurredAt | prettyDate }}
              </td>
              <td class="px-4 py-2.5 font-medium">
                {{ row.userDisplayName }}
              </td>
              <td class="px-4 py-2.5">
                <span
                  [class]="
                    'rounded px-2 py-0.5 text-xs font-medium ' +
                    pillClass(row.type)
                  ">
                  {{ row.type | activityType }}
                </span>
              </td>
              <td class="text-foreground/80 px-4 py-2.5">
                {{ row.entityType | entityType }}
                @if (row.entityId) {
                  <span class="text-foreground/50"> #{{ row.entityId }}</span>
                }
              </td>
              <td class="text-foreground/60 px-4 py-2.5 text-xs">
                @if (row.projectSlug) {
                  <span>{{ row.projectSlug }}</span>
                }
                @if (row.boardSlug) {
                  <span class="ml-1">/ {{ row.boardSlug }}</span>
                }
              </td>
            </tr>
          } @empty {
            <tr>
              <td appTableEmptyCell colspan="5">No audit events found.</td>
            </tr>
          }
        </tbody>

        <app-table-pagination
          itemLabel="events"
          [page]="state.currentPage()"
          [pageSize]="state.pageSize()"
          [pageSizeOptions]="[10, 25, 50, 100]"
          [totalItems]="state.totalCount()"
          [totalPages]="state.totalPages()"
          (pageChange)="state.goToPage($event)"
          (pageSizeChange)="state.setPageSize($event)" />
      </app-table>
    }
  `,
})
export class AuditTableComponent {
  protected state = inject(AuditStore);

  protected pillClass(type: ActivityType): string {
    switch (type) {
      case ActivityType.create:
      case ActivityType.addTag:
      case ActivityType.loginSuccess:
        return 'bg-green-500/10 text-green-600 dark:text-green-400';
      case ActivityType.delete:
      case ActivityType.remove:
      case ActivityType.removeTag:
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
