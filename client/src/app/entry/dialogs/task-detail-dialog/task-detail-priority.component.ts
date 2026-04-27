import { NgClass } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  viewChild,
} from '@angular/core';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
  taskPriorityOptions,
} from '@core/enums/task-priority';
import { selectRequiredDetailTask } from '@core/store/tasks/tasks.selectors';
import { LucideFlag } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-priority-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DropdownMenuComponent, MenuItemComponent, NgClass, LucideFlag],
  template: `
    <button
      class="flex cursor-pointer items-center gap-2 rounded-sm px-4 py-2 text-sm transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800"
      (click)="menu.toggle($any($event.currentTarget))">
      <svg lucideFlag class="h-4 w-4" [ngClass]="iconColor()"></svg>
      <span [ngClass]="labelColor()">{{ label() }}</span>
    </button>

    <app-dropdown-menu #menu>
      <small class="block px-3 py-1 text-xs text-neutral-500">
        Set Priority
      </small>
      @for (option of options; track option.value) {
        <button
          app-menu-item
          (click)="selectPriority(option.value); menu.close()">
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
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);
  readonly task = this.store.selectSignal(selectRequiredDetailTask);

  readonly priority = computed(() => this.task().priority);

  readonly options = taskPriorityOptions;

  readonly menu = viewChild.required(DropdownMenuComponent);

  label() {
    const p = this.priority() ?? TaskPriority.none;
    return taskPriorityLabels[p];
  }

  labelColor() {
    const p = this.priority() ?? TaskPriority.none;
    return taskPriorityColors[p];
  }

  iconColor() {
    const p = this.priority() ?? TaskPriority.none;
    return taskPriorityColors[p];
  }

  colorFor(priority: TaskPriority) {
    return taskPriorityColors[priority];
  }

  selectPriority(priority: TaskPriority) {
    const task = this.task();
    if (!task) return;
    this.taskDetailService.updateTask({ ...task, priority });
  }
}
