import { Component, computed, inject, input } from '@angular/core';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { LucidePencil } from '@lucide/angular';
import { DialogService } from '@core/services/dialog.service';
import { BulkEditTasksDialogComponent } from '@entry/dialogs/bulk-edit-tasks-dialog/bulk-edit-tasks-dialog.component';

@Component({
  selector: 'app-task-list-selection-actions',
  imports: [StrokedButtonComponent, LucidePencil],
  template: `
    @if (selectedCount() > 0) {
      <div class="ml-auto flex flex-row items-center gap-4">
        <span class="text-muted px-2 text-sm"
          >{{ selectedCount() }} selected</span
        >
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

  selection = input<number[]>([]);
  readonly selectedCount = computed(() => this.selection().length);

  bulkEditClicked() {
    this.dialog.open(BulkEditTasksDialogComponent, {
      width: BulkEditTasksDialogComponent.width,
      data: [...this.selection()],
      panelClass: 'app-modal-class',
    });
  }
}
