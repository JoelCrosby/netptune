import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AppUser } from '@app/core/models/appuser';
import { UpdateProjectTaskRequest } from '@app/core/models/requests/update-project-task-request';
import { selectAllProjects } from '@app/core/store/projects/projects.selectors';
import { selectRequiredDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { selectAllUsers } from '@app/core/store/users/users.selectors';
import { AvatarComponent } from '@app/static/components/avatar/avatar.component';
import { ChipListboxComponent } from '@app/static/components/chip/chip-listbox.component';
import { ChipOptionComponent } from '@app/static/components/chip/chip-option.component';
import { DropdownMenuComponent } from '@app/static/components/dropdown-menu/dropdown-menu.component';
import { UserSelectComponent } from '@app/static/components/user-select/user-select.component';
import { Store } from '@ngrx/store';
import { TaskStatusPipe } from '@static/pipes/task-status.pipe';
import { TaskDetailActionsComponent } from './task-detail-actions.component';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-properties',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    UserSelectComponent,
    AvatarComponent,
    ChipListboxComponent,
    DropdownMenuComponent,
    TaskDetailActionsComponent,
    TaskStatusPipe,
    ChipOptionComponent,
  ],
  template: `
    <div class="flex flex-col">
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Assignees</h4>
        <app-user-select
          [value]="task().assignees"
          [options]="users()"
          (selectChange)="selectAssignee($event)" />
      </div>
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Reporter</h4>
        <div class="flex flex-row items-center rounded">
          <app-avatar
            size="sm"
            [name]="task().ownerUsername"
            [imageUrl]="task().ownerPictureUrl">
          </app-avatar>
          <small class="ml-2 text-sm font-medium">{{
            task().ownerUsername
          }}</small>
        </div>
      </div>
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Status</h4>
        <app-chip-listbox>
          <app-chip-option>{{ task().status | taskStatus }}</app-chip-option>
        </app-chip-listbox>
      </div>
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Project</h4>
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
          @for (project of projects(); track project.id) {
            <button
              app-menu-item
              (click)="selectProject(project.id); projectsMenu.close()">
              {{ project.name }}
            </button>
          }
        </app-dropdown-menu>
      </div>
    </div>

    <app-task-detail-actions />
  `,
})
export class TaskDetailPropertiesComponent {
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);

  readonly task = this.store.selectSignal(selectRequiredDetailTask);
  readonly projects = this.store.selectSignal(selectAllProjects);
  readonly users = this.store.selectSignal(selectAllUsers);

  selectProject(projectId: number) {
    const task = this.task();

    if (!task) return;

    const updated: UpdateProjectTaskRequest = {
      ...task,
      projectId,
    };

    this.taskDetailService.updateTask(updated);
  }

  selectAssignee(user: AppUser) {
    const task = this.task();

    if (!task) return;

    const assigneeSet = new Set(task.assignees.map((u) => u.id));

    if (assigneeSet.has(user.id)) {
      assigneeSet.delete(user.id);
    } else {
      assigneeSet.add(user.id);
    }

    const assigneeIds = Array.from(assigneeSet);
    const updated: UpdateProjectTaskRequest = {
      ...task,
      assigneeIds,
    };

    this.taskDetailService.updateTask(updated);
  }
}
