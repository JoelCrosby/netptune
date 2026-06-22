import { httpResource } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { MAX_PAGE_SIZE } from '@app/core/models/pagination';
import { ProjectViewModel } from '@app/core/models/view-models/project-view-model';
import { selectRequiredDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { ChipListboxComponent } from '@app/static/components/chip/chip-listbox.component';
import { ChipOptionComponent } from '@app/static/components/chip/chip-option.component';
import { DropdownMenuComponent } from '@app/static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@app/static/components/dropdown-menu/menu-item.component';
import { Store } from '@ngrx/store';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-project-select',
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
        (click)="projectsMenu.toggle($any($event.currentTarget))">
        {{ task().projectName }}
      </button>
    </app-chip-listbox>
    <app-dropdown-menu #projectsMenu>
      <small class="block px-3 py-1 text-xs text-neutral-500">
        Change Project
      </small>
      @for (project of projects.value(); track project.id) {
        <button
          app-menu-item
          (click)="selectProject(project.id); projectsMenu.close()">
          {{ project.name }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskDetailProjectSelectComponent {
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);
  readonly task = this.store.selectSignal(selectRequiredDetailTask);

  readonly projects = httpResource<ProjectViewModel[]>(
    () => ({
      url: 'api/projects',
      params: {
        page: 1,
        pageSize: MAX_PAGE_SIZE,
      },
    }),
    { defaultValue: [] }
  );

  selectProject(projectId: number) {
    const task = this.task();
    if (!task) return;
    this.taskDetailService.updateTask({ ...task, projectId });
  }
}
