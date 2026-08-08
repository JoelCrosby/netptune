import { Component, computed, inject } from '@angular/core';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { LucideSettings2, LucideTrash } from '@lucide/angular';
import { DialogService } from '@core/services/dialog.service';
import { BulkEditTasksDialogComponent } from '@entry/dialogs/bulk-edit-tasks-dialog/bulk-edit-tasks-dialog.component';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { TaskSelectionService } from '@core/services/task-selection.service';

@Component({
  selector: 'app-task-list-selection-actions',
  imports: [StrokedButtonComponent, LucideSettings2, LucideTrash],
  template: `
    @if (selectedCount() > 0) {
      <div class="ml-auto flex flex-row items-center gap-4">
        <span class="text-muted px-2 text-sm">
          <span
            i18n="
              Count of selected rows above a table. COUNT is the number selected
            ">
            {{
              selectedCount() // i18n(ph="COUNT")
            }}
            selected
          </span>
        </span>
        <button
          app-stroked-button
          color="warn"
          type="button"
          (click)="deleteClicked()">
          <svg lucideTrash class="h-4 w-4"></svg>
          <span i18n="Button that deletes the selected tasks">Delete</span>
        </button>
        <button app-stroked-button type="button" (click)="bulkEditClicked()">
          <svg lucideSettings2 class="h-4 w-4"></svg>
          <span
            i18n="
              Button that opens the bulk edit dialog for the selected tasks
            ">
            Bulk edit
          </span>
        </button>
      </div>
    }
  `,
})
export class TaskListSelectionActionsComponent {
  private readonly dialog = inject(DialogService);

  private readonly taskCommands = inject(TaskCommandsService);
  private readonly taskSelection = inject(TaskSelectionService);

  readonly selection = this.taskSelection.taskIds;
  readonly selectedCount = computed(() => this.selection().length);

  bulkEditClicked() {
    this.dialog.open(BulkEditTasksDialogComponent, {
      width: BulkEditTasksDialogComponent.width,
      data: [...this.selection()],
      panelClass: 'app-modal-class',
    });
  }

  deleteClicked() {
    const ids = this.selection();

    if (ids.length === 0) return;

    this.taskCommands.deleteMany([...ids]);
  }
}
