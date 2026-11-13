import { Component, computed, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { BoardGroupUsersComponent } from '@boards/components/board-group-users/board-group-users.component';
import { BoardGroupsSearchComponent } from '@boards/components/board-groups-search/board-groups-search.component';
import { BoardGroupSortComponent } from '@boards/components/board-group-sort/board-group-sort.component';
import { BoardGroupStatusComponent } from '@boards/components/board-group-status/board-group-status.component';
import { BoardGroupColumnsComponent } from '@boards/components/board-group-columns/board-group-columns.component';
import { BoardFiltersDialogComponent } from '@boards/components/board-filters-dialog/board-filters-dialog.component';
import { TagFilterContainerComponent } from '@shared/components/tag-filter/tag-filter-container.component';
import { BoardGroupHeaderSeperatorComponent } from './board-group-header-seperator.component';
import { LucideListFilter } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { ToolbarButtonComponent } from '@static/components/button/toolbar-button.component';
import { PERMISSIONS } from '@app/core/auth/permissions';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { DialogService } from '@core/services/dialog.service';

@Component({
  selector: 'app-board-group-header',
  host: { class: 'block min-w-0 flex-1' },
  imports: [
    BoardGroupsSearchComponent,
    BoardGroupUsersComponent,
    TagFilterContainerComponent,
    BoardGroupStatusComponent,
    BoardGroupSortComponent,
    BoardGroupHeaderSeperatorComponent,
    BoardGroupColumnsComponent,
    BadgeComponent,
    ToolbarButtonComponent,
    LucideListFilter,
  ],
  template: `
    <div
      class="@container -my-1.5 flex min-w-0 flex-row items-center gap-2 overflow-x-auto py-1.5">
      <app-board-group-header-seperator />
      <app-board-groups-search />

      <app-board-group-header-seperator class="hidden @2xl:block" />
      <app-board-group-users class="hidden @2xl:block" />

      @if (showFilters()) {
        <app-board-group-header-seperator class="hidden @2xl:block" />

        <div class="hidden flex-row items-center gap-1 @2xl:flex">
          @if (readTags()) {
            <app-tag-filter-container />
          }

          @if (readStatus()) {
            <app-board-group-status />
          }

          @if (hasFilters()) {
            <button app-toolbar-button (click)="onClearFiltersClicked()">
              <span i18n="Button that removes every filter applied to the board">
                Clear all
              </span>
            </button>
          }
        </div>
      }

      <button
        app-toolbar-button
        class="@2xl:hidden"
        (click)="onOpenFiltersClicked()">
        <svg lucideListFilter class="h-4 w-4" aria-hidden="true"></svg>
        <span i18n="Button that opens the board filters on a small screen">
          Filter
        </span>
        @if (activeFilterCount(); as count) {
          <app-badge color="primary">{{ count }}</app-badge>
        }
      </button>

      <app-board-group-header-seperator class="hidden @2xl:block" />

      <div class="hidden flex-row items-center gap-1 @2xl:flex">
        <app-board-group-sort />
        <app-board-group-columns />
      </div>
    </div>
  `,
})
export class BoardGroupHeaderComponent {
  private dialog = inject(DialogService);
  private readonly filterRoute = taskFilterRoute();

  protected readonly hasFilters = this.filterRoute.hasFilters;

  readStatus = hasPermission(PERMISSIONS.statuses.read);

  readTags = hasPermission(PERMISSIONS.tags.read);

  protected readonly showFilters = computed(() => {
    return this.readTags() || this.readStatus();
  });

  protected readonly activeFilterCount = computed(() => {
    const filters = this.filterRoute.filters();

    const byTag = !!filters.tags?.length || filters.hasTags !== undefined;
    const byUser = !!filters.users?.length;
    const byStatus = !!filters.statuses?.length;

    return [byTag, byUser, byStatus].filter(Boolean).length;
  });

  onClearFiltersClicked() {
    this.filterRoute.clear();
  }

  onOpenFiltersClicked() {
    this.dialog.open(BoardFiltersDialogComponent, {
      width: '420px',
      maxWidth: 'calc(100vw - 32px)',
    });
  }
}
