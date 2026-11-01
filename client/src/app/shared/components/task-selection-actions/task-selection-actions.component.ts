import { Component, computed, inject } from '@angular/core';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { SelectionActionsComponent } from '@static/components/selection-actions/selection-actions.component';
import { LucideSettings2, LucideTrash } from '@lucide/angular';
import { DialogService } from '@core/services/dialog.service';
import { BulkEditTasksDialogComponent } from '@entry/dialogs/bulk-edit-tasks-dialog/bulk-edit-tasks-dialog.component';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { TaskSelectionService } from '@core/services/task-selection.service';

@Component({
  selector: 'app-task-selection-actions',
  imports: [
    LucideSettings2,
    LucideTrash,
    SelectionActionsComponent,
    StrokedButtonComponent,
  ],
  template: `
    @if (selectedCount() > 0) {
      <app-selection-actions [count]="selectedCount()">
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
      </app-selection-actions>
    }
  `,
})
export class TaskSelectionActionsComponent {
  private readonly dialog = inject(DialogService);

  private readonly taskCommands = inject(TaskCommandsService);
  private readonly taskSelection = inject(TaskSelectionService);

  readonly selection = this.taskSelection.tasks;
  readonly selectedCount = computed(() => this.selection().length);

  bulkEditClicked() {
    this.dialog.open(BulkEditTasksDialogComponent, {
      width: BulkEditTasksDialogComponent.width,
      data: [...this.selection()],
      panelClass: BulkEditTasksDialogComponent.panelClass,
    });
  }

  deleteClicked() {
    const ids = this.taskSelection.taskIds();

    if (ids.length === 0) return;

    this.taskCommands.deleteMany([...ids], () => this.taskSelection.clear());
  }
}
