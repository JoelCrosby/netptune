import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
} from '@angular/core';
import { TaskStatus } from '@core/enums/project-task-status';
import { toggleSelectedStatus } from '@core/store/tasks/tasks.actions';
import {
  SelectedTaskStatus,
  selectSelectedTaskStatusCount,
  selectTaskStatusOptions,
} from '@core/store/tasks/tasks.selectors';
import { LucideCircleDashed } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { TaskListFilterActionComponent } from './task-list-filter-action.component';

@Component({
  selector: 'app-task-list-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TaskListFilterActionComponent,
    DropdownMenuComponent,
    MenuCheckboxItemComponent,
  ],
  template: `
    <app-task-list-filter-action
      label="Filter by Status"
      [icon]="lucideCircleDashed"
      [color]="selectedCount() ? 'primary' : undefined"
      (action)="menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu>
      @for (status of statuses(); track trackByStatus($index, status)) {
        <button
          app-menu-checkbox-item
          [checked]="status.selected"
          (checkedChange)="onOptionClicked(status.status)">
          {{ status.label }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskListStatusComponent {
  private readonly store = inject(Store);
  readonly el = inject(ElementRef);

  readonly lucideCircleDashed = LucideCircleDashed;

  readonly statuses = this.store.selectSignal(selectTaskStatusOptions);
  readonly selectedCount = this.store.selectSignal(
    selectSelectedTaskStatusCount
  );

  trackByStatus(_: number, status: SelectedTaskStatus) {
    return status.status;
  }

  onOptionClicked(status: TaskStatus) {
    this.store.dispatch(toggleSelectedStatus({ status }));
  }
}
