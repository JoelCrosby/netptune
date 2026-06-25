import { Component, computed, inject } from '@angular/core';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { LucidePencil } from '@lucide/angular';
import { DialogService } from '@core/services/dialog.service';
import { BulkEditTasksDialogComponent } from '@entry/dialogs/bulk-edit-tasks-dialog/bulk-edit-tasks-dialog.component';
import { TaskListSelectionService } from './task-list-selection.service';

@Component({
  selector: 'app-task-list-selection-actions',
  imports: [StrokedButtonComponent, LucidePencil],
  template: `
    @if (selectedCount() > 0) {
      <div class="ml-auto flex flex-row items-center gap-4">
        <span class="text-muted text-sm">{{ selectedCount() }} selected</span>
        <button app-stroked-button type="button" (click)="bulkEditClicked()">
          <svg lucidePencil class="h-4 w-4"></svg>
          <span>Bulk edit</span>
        </button>
      </div>
    }
  `,
})
export class TaskListSelectionActionsComponent {
  private readonly selection = inject(TaskListSelectionService);
  private readonly dialog = inject(DialogService);

  readonly selectedTasks = this.selection.selectedTasks;
  readonly selectedCount = computed(() => this.selectedTasks().length);

  bulkEditClicked() {
    this.dialog.open(BulkEditTasksDialogComponent, {
      width: BulkEditTasksDialogComponent.width,
      data: [...this.selectedTasks()],
      panelClass: 'app-modal-class',
    });
  }
}
