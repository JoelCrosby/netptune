import { Component, computed, inject } from '@angular/core';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { LucidePencil, LucideTrash } from '@lucide/angular';
import { DialogService } from '@core/services/dialog.service';
import { BulkEditTasksDialogComponent } from '@entry/dialogs/bulk-edit-tasks-dialog/bulk-edit-tasks-dialog.component';
import { Store } from '@ngrx/store';
import { bulkDeleteTasksAction } from '@core/store/tasks/tasks.actions';
import { selectSelectedTaskIds } from '@core/store/tasks/tasks.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';

@Component({
  selector: 'app-task-list-selection-actions',
  imports: [StrokedButtonComponent, LucidePencil, LucideTrash],
  template: `
    @if (selectedCount() > 0) {
      <div class="ml-auto flex flex-row items-center gap-4">
        <span class="text-muted px-2 text-sm"
          >{{ selectedCount() }} selected</span
        >
        <button
          app-stroked-button
          color="warn"
          type="button"
          (click)="deleteClicked()">
          <svg lucideTrash class="h-4 w-4"></svg>
          <span>Delete</span>
        </button>
        <button app-stroked-button type="button" (click)="bulkEditClicked()">
          <svg lucidePencil class="h-4 w-4"></svg>
          <span>Bulk edit</span>
        </button>
      </div>
    }
  `,
})
export class TaskListSelectionActionsComponent {
  private readonly dialog = inject(DialogService);
  private readonly store = inject(Store);

  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  readonly selection = this.store.selectSignal(selectSelectedTaskIds);
  readonly selectedCount = computed(() => this.selection().length);

  bulkEditClicked() {
    this.dialog.open(BulkEditTasksDialogComponent, {
      width: BulkEditTasksDialogComponent.width,
      data: [...this.selection()],
      panelClass: 'app-modal-class',
    });
  }

  deleteClicked() {
    const workspaceId = this.workspaceId();
    const ids = this.selection();

    if (!workspaceId || ids.length === 0) return;

    this.store.dispatch(
      bulkDeleteTasksAction.init({
        identifier: `[workspace] ${workspaceId}`,
        ids: [...ids],
      })
    );
  }
}
