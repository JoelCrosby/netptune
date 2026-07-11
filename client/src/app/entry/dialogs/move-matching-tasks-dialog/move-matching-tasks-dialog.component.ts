import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

export interface MoveMatchingTask {
  id: number;
  name: string;
  systemId: string;
  groupName: string;
}

export interface MoveMatchingTasksDialogData {
  groupName: string;
  statusName: string;
  tasks: MoveMatchingTask[];
}

@Component({
  selector: 'app-move-matching-tasks-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    CheckboxComponent,
    TaskScopeIdComponent,
  ],
  template: `
    <app-dialog-title>Move matching tasks</app-dialog-title>

    <div class="flex w-full max-w-full min-w-0 flex-col gap-4">
      <p class="text-muted text-sm">
        {{ data.tasks.length }}
        {{ data.tasks.length === 1 ? 'task has' : 'tasks have' }} the status
        <span class="font-medium">{{ data.statusName }}</span>
        . Choose which to move into
        <span class="font-medium">{{ data.groupName }}</span>
        .
      </p>

      <div
        class="border-border mx-2 grid grid-cols-[auto_7rem_1fr_12rem] items-center gap-3 border-b pb-3">
        <app-checkbox
          [checked]="allSelected()"
          (changed)="toggleAll($event)"
          [attr.aria-label]="'Select all tasks'" />
        <span class="text-muted text-xs font-semibold">Key</span>
        <span class="text-muted text-xs font-semibold">Task</span>
        <span class="text-muted text-xs font-semibold">Current group</span>
      </div>

      <div class="flex max-h-96 flex-col gap-2 overflow-y-auto">
        @for (task of data.tasks; track task.id) {
          <div
            role="checkbox"
            tabindex="0"
            class="hover:bg-hover focus-visible:ring-primary grid cursor-pointer grid-cols-[auto_7rem_1fr_12rem] items-center gap-3 rounded px-2 py-2 transition-colors select-none focus-visible:ring-2 focus-visible:outline-none"
            [attr.aria-checked]="isSelected(task.id)"
            [attr.aria-label]="task.name"
            (click)="toggle(task.id, !isSelected(task.id))"
            (keydown.space)="onRowKeydown($event, task.id)"
            (keydown.enter)="onRowKeydown($event, task.id)">
            <app-checkbox
              class="pointer-events-none"
              [checked]="isSelected(task.id)" />
            <app-task-scope-id class="w-16 truncate" [id]="task.systemId" />
            <span class="min-w-0 truncate text-sm font-medium">
              {{ task.name }}
            </span>
            <span class="text-muted min-w-0 truncate text-xs">
              {{ task.groupName }}
            </span>
          </div>
        }
      </div>
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">Cancel</button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="selectedIds().size === 0"
        (click)="move()">
        Move
        {{ selectedIds().size }}
        {{ selectedIds().size === 1 ? 'task' : 'tasks' }}
      </button>
    </div>
  `,
})
export class MoveMatchingTasksDialogComponent {
  static readonly width = '960px';

  private dialogRef =
    inject<DialogRef<number[] | undefined, MoveMatchingTasksDialogComponent>>(
      DialogRef
    );

  readonly data = inject<MoveMatchingTasksDialogData>(DIALOG_DATA);

  readonly selectedIds = signal(
    new Set(this.data.tasks.map((task) => task.id))
  );

  readonly allSelected = computed(
    () => this.selectedIds().size === this.data.tasks.length
  );

  isSelected(taskId: number) {
    return this.selectedIds().has(taskId);
  }

  toggle(taskId: number, selected: boolean) {
    this.selectedIds.update((ids) => {
      const next = new Set(ids);

      if (selected) {
        next.add(taskId);
      } else {
        next.delete(taskId);
      }

      return next;
    });
  }

  // Space would scroll the list and Enter would submit the dialog.
  onRowKeydown(event: Event, taskId: number) {
    event.preventDefault();
    this.toggle(taskId, !this.isSelected(taskId));
  }

  toggleAll(selected: boolean) {
    this.selectedIds.set(
      selected ? new Set(this.data.tasks.map((task) => task.id)) : new Set()
    );
  }

  move() {
    this.dialogRef.close([...this.selectedIds()]);
  }

  close() {
    this.dialogRef.close();
  }
}
