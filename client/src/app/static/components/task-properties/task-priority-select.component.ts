import { NgClass } from '@angular/common';
import { Component, model } from '@angular/core';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
  taskPriorityOptions,
} from '@core/enums/task-priority';
import { LucideFlag } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-priority-select',
  imports: [DropdownMenuComponent, MenuItemComponent, NgClass, LucideFlag],
  template: `
    <button
      type="button"
      class="flex cursor-pointer items-center gap-2 rounded-sm px-4 py-2 text-sm transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800"
      (click)="menu.toggle($any($event.currentTarget))">
      <svg lucideFlag class="h-4 w-4" [ngClass]="colorFor(priority())"></svg>
      <span [ngClass]="colorFor(priority())">{{ label() }}</span>
    </button>

    <app-dropdown-menu #menu>
      <small class="block px-3 py-1 text-xs text-neutral-500">
        Set Priority
      </small>
      @for (option of options; track option.value) {
        <button app-menu-item (click)="value.set(option.value); menu.close()">
          <svg
            lucideFlag
            class="h-4 w-4"
            [ngClass]="colorFor(option.value)"></svg>
          {{ option.label }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskPrioritySelectComponent {
  readonly value = model<TaskPriority | null>(null);

  readonly options = taskPriorityOptions;

  priority() {
    return this.value() ?? TaskPriority.none;
  }

  label() {
    return taskPriorityLabels[this.priority()];
  }

  colorFor(priority: TaskPriority) {
    return taskPriorityColors[priority];
  }
}
