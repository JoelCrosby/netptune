import { Component, input, output } from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { LucideCheck, LucideTrash2, LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { UserSelectComponent } from '@static/components/user-select/user-select.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-notifications-filters',
  imports: [
    TooltipDirective,
    FlatButtonComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    SearchInputComponent,
    UserSelectComponent,
    LucideCheck,
    LucideTrash2,
    LucideX,
  ],
  template: `
    <div class="mb-3 flex h-18 flex-wrap items-center gap-3 py-2">
      <app-search-input
        [term]="searchTerm()"
        (searchChange)="searchChange.emit($event)" />

      <app-user-select
        class="min-w-44"
        [options]="users()"
        [value]="selectedUsers()"
        i18n-label="Filter option including every member"
        label="All users"
        (selectChange)="userFilter.emit($event)" />

      @if (selectedUsers().length) {
        <button
          app-icon-button
          type="button"
          i18n-aria-label="
            Accessible label for the button that clears the member filter
          "
          aria-label="Clear user filter"
          i18n-appTooltip="Tooltip on the button that clears the member filter"
          appTooltip="Clear user filter"
          (click)="clearUserFilter.emit()">
          <svg lucideX class="text-foreground/50 h-4 w-4"></svg>
        </button>
      }

      @if (selectedCount()) {
        <div class="ml-auto flex items-center gap-2">
          <span class="text-sm font-medium">
            <span i18n="Count of selected rows. COUNT is the number selected">
              {{
                selectedCount() // i18n(ph="COUNT")
              }}
              selected
            </span>
          </span>
          <button
            app-stroked-button
            type="button"
            (click)="markSelectedAsRead.emit()">
            <svg lucideCheck class="mr-1.5 inline h-4 w-4"></svg>
            <span i18n="Button that marks the selected notifications as read">
              Mark as read
            </span>
          </button>
          <button
            app-flat-button
            color="warn"
            type="button"
            (click)="deleteSelected.emit()">
            <svg lucideTrash2 class="mr-1.5 inline h-4 w-4"></svg>
            <span i18n="Button that deletes the selected notifications">
              Delete
            </span>
          </button>
        </div>
      }
    </div>
  `,
})
export class NotificationsFiltersComponent {
  readonly searchTerm = input<string | null>(null);
  readonly users = input<AppUser[] | null>([]);
  readonly selectedUsers = input<AppUser[]>([]);
  readonly selectedCount = input(0);

  readonly searchChange = output<string | null>();
  readonly userFilter = output<AppUser>();
  readonly clearUserFilter = output();
  readonly markSelectedAsRead = output();
  readonly deleteSelected = output();
}
