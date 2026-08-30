import { Component, input, model } from '@angular/core';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityOptions,
} from '@core/enums/task-priority';
import { LucideFlag } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-priority-picker',
  imports: [DropdownMenuComponent, MenuItemComponent, LucideFlag],
  template: `
    <button
      type="button"
      [class]="buttonClass()"
      [disabled]="disabled()"
      [attr.aria-label]="ariaLabel"
      aria-haspopup="menu"
      (click)="menu.toggle($any($event.currentTarget))">
      <ng-content />
    </button>

    <app-dropdown-menu #menu>
      <small class="text-muted block px-3 py-1 text-xs">
        <span i18n="Heading above the task priority options">Set Priority</span>
      </small>
      @for (option of options; track option.value) {
        <button app-menu-item (click)="value.set(option.value); menu.close()">
          <svg
            lucideFlag
            class="h-4 w-4"
            [class]="colorFor(option.value)"></svg>
          {{ option.label }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskPriorityPickerComponent {
  readonly value = model<TaskPriority | null>(null);
  readonly disabled = input(false);
  readonly buttonClass = input('');

  readonly options = taskPriorityOptions;

  readonly ariaLabel = $localize`:Accessible label for the control that changes a task's priority:Set priority`;

  colorFor(priority: TaskPriority) {
    return taskPriorityColors[priority];
  }
}
