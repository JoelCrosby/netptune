import { Component } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { BoardGroupColumnsComponent } from '@boards/components/board-group-columns/board-group-columns.component';
import { BoardGroupSortComponent } from '@boards/components/board-group-sort/board-group-sort.component';
import { BoardGroupStatusComponent } from '@boards/components/board-group-status/board-group-status.component';
import { BoardGroupUsersComponent } from '@boards/components/board-group-users/board-group-users.component';
import { TagFilterContainerComponent } from '@shared/components/tag-filter/tag-filter-container.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FieldRowComponent } from '@static/components/field-row/field-row.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  selector: 'app-board-filters-dialog',
  imports: [
    BoardGroupColumnsComponent,
    BoardGroupSortComponent,
    BoardGroupStatusComponent,
    BoardGroupUsersComponent,
    TagFilterContainerComponent,
    DialogTitleComponent,
    DialogContentComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FieldRowComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title
      showCloseButton
      i18n="Title of the dialog that holds the board filters">
      Filters
    </app-dialog-title>

    <app-dialog-content>
      <div class="flex flex-col gap-1">
        <app-field-row
          mode="container"
          i18n-label="Row label for the assignee filter in the filters dialog"
          label="Assignees">
          <div class="min-w-0 overflow-x-auto">
            <app-board-group-users />
          </div>
        </app-field-row>

        @if (readTags()) {
          <app-field-row
            mode="container"
            i18n-label="Row label for the tag filter in the filters dialog"
            label="Tags">
            <app-tag-filter-container />
          </app-field-row>
        }

        @if (readStatus()) {
          <app-field-row
            mode="container"
            i18n-label="Row label for the status filter in the filters dialog"
            label="Status">
            <app-board-group-status />
          </app-field-row>
        }

        <app-field-row
          mode="container"
          i18n-label="Row label for the task sort control in the filters dialog"
          label="Sort">
          <app-board-group-sort />
        </app-field-row>

        <app-field-row
          mode="container"
          i18n-label="Row label for the column control in the filters dialog"
          label="Columns">
          <app-board-group-columns />
        </app-field-row>
      </div>
    </app-dialog-content>

    <div app-dialog-actions align="end">
      <button
        app-stroked-button
        color="neutral"
        type="button"
        class="mr-auto"
        [disabled]="!hasFilters()"
        (click)="onClearClicked()">
        <span i18n="Button that removes every filter applied to the board">
          Clear all
        </span>
      </button>

      <button app-flat-button color="primary" app-dialog-close>
        <span i18n="Button that closes the board filters dialog">Done</span>
      </button>
    </div>
  `,
})
export class BoardFiltersDialogComponent {
  private readonly filterRoute = taskFilterRoute();

  protected readonly readStatus = hasPermission(PERMISSIONS.statuses.read);
  protected readonly readTags = hasPermission(PERMISSIONS.tags.read);

  protected readonly hasFilters = this.filterRoute.hasFilters;

  onClearClicked() {
    this.filterRoute.clear();
  }
}
