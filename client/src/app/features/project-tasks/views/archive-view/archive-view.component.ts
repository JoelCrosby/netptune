import { Component, computed, viewChild } from '@angular/core';
import { LucideArchiveRestore } from '@lucide/angular';
import { ArchiveListComponent } from '@project-tasks/components/archive-list/archive-list.component';
import { TaskListFiltersComponent } from '@project-tasks/components/task-list/task-list-filters.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SelectionActionsComponent } from '@static/components/selection-actions/selection-actions.component';

@Component({
  selector: 'app-archive-view',
  imports: [
    ArchiveListComponent,
    LucideArchiveRestore,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    SelectionActionsComponent,
    StrokedButtonComponent,
    TaskListFiltersComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the archived task list"
        title="Archive"
        i18n-filtersLabel="Accessible name of the archived task filter row"
        filtersLabel="Filter archived tasks"
        [count]="count()">
        <app-task-list-filters pageHeaderFilters showProjectSprint>
          <div selectionActions class="ml-auto">
            @if (selectedCount() > 0) {
              <app-selection-actions [count]="selectedCount()">
                <button
                  app-stroked-button
                  type="button"
                  (click)="restoreSelected()">
                  <svg lucideArchiveRestore class="h-4 w-4"></svg>
                  <span i18n="Button that restores the selected archived tasks">
                    Restore
                  </span>
                </button>
              </app-selection-actions>
            }
          </div>
        </app-task-list-filters>
      </app-page-header>

      <app-page-body>
        <app-archive-list />
      </app-page-body>
    </app-page-container>
  `,
})
export class ArchiveViewComponent {
  private readonly list = viewChild(ArchiveListComponent);
  readonly count = computed(() => this.list()?.count() ?? null);
  readonly selectedCount = computed(() => this.list()?.selectedCount() ?? 0);

  restoreSelected() {
    this.list()?.restoreSelected();
  }
}
