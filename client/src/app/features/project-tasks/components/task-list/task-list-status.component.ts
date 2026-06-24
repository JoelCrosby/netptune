import { Component, ElementRef, inject } from '@angular/core';
import { toggleSelectedStatus } from '@core/store/tasks/tasks.actions';
import {
  selectSelectedTaskStatusCount,
  selectTaskStatusOptions,
} from '@core/store/tasks/tasks.selectors';
import { LucideCircleDashed } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { TaskListFilterActionComponent } from './task-list-filter-action.component';
import { statusResource } from '@app/core/resources/status.resources';

@Component({
  selector: 'app-task-list-status',
  imports: [
    TaskListFilterActionComponent,
    DropdownMenuComponent,
    MenuCheckboxItemComponent,
  ],
  template: `
    @let set = selected();

    <app-task-list-filter-action
      label="Filter by Status"
      [icon]="lucideCircleDashed"
      [color]="selectedCount() ? 'primary' : undefined"
      (action)="menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu>
      @for (status of statuses.value(); track status.id) {
        <button
          app-menu-checkbox-item
          [checked]="set.has(status.id)"
          (checkedChange)="onOptionClicked(status.id)">
          {{ status.name }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskListStatusComponent {
  private readonly store = inject(Store);
  readonly el = inject(ElementRef);

  readonly lucideCircleDashed = LucideCircleDashed;

  readonly selected = this.store.selectSignal(selectTaskStatusOptions);
  readonly statuses = statusResource();
  readonly selectedCount = this.store.selectSignal(
    selectSelectedTaskStatusCount
  );

  onOptionClicked(status: number) {
    this.store.dispatch(toggleSelectedStatus({ status }));
  }
}
