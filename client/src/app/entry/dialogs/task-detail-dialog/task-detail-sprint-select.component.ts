import { httpResource } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { SprintStatus, sprintStatusLabels } from '@core/enums/sprint-status';
import {
  selectRequiredDetailTask,
  selectTaskEditLoading,
} from '@core/store/tasks/tasks.selectors';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { Store } from '@ngrx/store';
import { ChipListboxComponent } from '@static/components/chip/chip-listbox.component';
import { ChipOptionComponent } from '@static/components/chip/chip-option.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-sprint-select',
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
        [loading]="updateLoading()"
        (click)="sprintsMenu.toggle($any($event.currentTarget))">
        {{ task().sprintName ?? 'No Sprint' }}
      </button>
    </app-chip-listbox>
    <app-dropdown-menu #sprintsMenu>
      <small class="block px-3 py-1 text-xs text-neutral-500">
        Change Sprint
      </small>
      @if (task().sprintId) {
        <button app-menu-item (click)="clearSprint(); sprintsMenu.close()">
          No Sprint
        </button>
      }
      @for (sprint of sprints.value(); track sprint.id) {
        <button
          app-menu-item
          [disabled]="sprint.id === task().sprintId"
          (click)="selectSprint(sprint.id); sprintsMenu.close()">
          <span>{{ sprint.name }}</span>
          <span class="text-xs text-neutral-500">
            {{ sprintStatusLabels[sprint.status] }}
          </span>
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskDetailSprintSelectComponent {
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);
  readonly task = this.store.selectSignal(selectRequiredDetailTask);
  readonly updateLoading = this.store.selectSignal(selectTaskEditLoading);

  readonly sprintStatusLabels = sprintStatusLabels;

  readonly sprints = httpResource<SprintViewModel[]>(
    () => ({
      url: 'api/sprints',
      params: {
        projectId: this.task().projectId,
        statuses: [SprintStatus.planning, SprintStatus.active],
        take: 100,
      },
    }),
    { defaultValue: [] }
  );

  selectSprint(sprintId: number) {
    if (sprintId === this.task().sprintId) return;
    this.taskDetailService.assignSprint(sprintId);
  }

  clearSprint() {
    this.taskDetailService.clearSprint();
  }
}
