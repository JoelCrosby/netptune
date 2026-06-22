import { httpResource } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { EntityType } from '@app/core/models/entity-type';
import { Status } from '@app/core/models/status';
import {
  selectRequiredDetailTask,
  selectTaskEditLoading,
} from '@app/core/store/tasks/tasks.selectors';
import { ChipListboxComponent } from '@app/static/components/chip/chip-listbox.component';
import { ChipOptionComponent } from '@app/static/components/chip/chip-option.component';
import { DropdownMenuComponent } from '@app/static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@app/static/components/dropdown-menu/menu-item.component';
import { Store } from '@ngrx/store';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-status-select',
  imports: [
    ChipListboxComponent,
    ChipOptionComponent,
    DropdownMenuComponent,
    MenuItemComponent,
  ],
  template: `
    <app-chip-listbox>
      <button
        app-chip-option
        [showChevron]="!updateLoading()"
        [disabled]="updateLoading()"
        [loading]="updateLoading()"
        (click)="statusMenu.toggle($any($event.currentTarget))">
        {{ task().statusName }}
      </button>
    </app-chip-listbox>
    <app-dropdown-menu #statusMenu>
      <small class="block px-3 py-1 text-xs text-neutral-500">
        Change Status
      </small>
      @for (status of statuses.value(); track status.id) {
        <button
          app-menu-item
          [disabled]="status.id === task().statusId || updateLoading()"
          (click)="selectStatus(status); statusMenu.close()">
          @if (status.color) {
            <span
              class="h-2.5 w-2.5 rounded-full"
              [style.background-color]="status.color"></span>
          }
          {{ status.name }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskDetailStatusSelectComponent {
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);
  readonly task = this.store.selectSignal(selectRequiredDetailTask);
  readonly updateLoading = this.store.selectSignal(selectTaskEditLoading);

  readonly statuses = httpResource<Status[]>(
    () => ({
      url: 'api/statuses',
      params: { entityType: EntityType.task },
    }),
    { defaultValue: [] }
  );

  selectStatus(status: Status) {
    if (status.id === this.task().statusId) return;
    this.taskDetailService.updateTask({ statusId: status.id });
  }
}
