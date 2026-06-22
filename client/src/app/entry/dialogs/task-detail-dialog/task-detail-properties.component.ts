import { Component, inject } from '@angular/core';
import { AppUser } from '@app/core/models/appuser';
import { UpdateProjectTaskRequest } from '@app/core/models/requests/update-project-task-request';
import { selectRequiredDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { selectAllUsers } from '@app/core/store/users/users.selectors';
import { TaskPrioritySelectComponent } from '@app/entry/dialogs/task-detail-dialog/task-detail-priority.component';
import { AvatarComponent } from '@app/static/components/avatar/avatar.component';
import { UserSelectComponent } from '@app/static/components/user-select/user-select.component';
import { Store } from '@ngrx/store';
import { TaskDetailEstimateComponent } from './task-detail-estimate.component';
import { TaskDetailProjectSelectComponent } from './task-detail-project-select.component';
import { TaskDetailSprintSelectComponent } from './task-detail-sprint-select.component';
import { TaskDetailStatusSelectComponent } from './task-detail-status-select.component';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-properties',
  imports: [
    UserSelectComponent,
    AvatarComponent,
    TaskPrioritySelectComponent,
    TaskDetailEstimateComponent,
    TaskDetailProjectSelectComponent,
    TaskDetailSprintSelectComponent,
    TaskDetailStatusSelectComponent,
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
        <div class="flex flex-row items-center rounded pl-2">
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
        <app-task-detail-status-select />
      </div>
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Priority</h4>
        <app-task-priority-select />
      </div>
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Estimate</h4>
        <app-task-detail-estimate />
      </div>
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Project</h4>
        <app-task-detail-project-select />
      </div>
      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Sprint</h4>
        <app-task-detail-sprint-select />
      </div>
    </div>
  `,
})
export class TaskDetailPropertiesComponent {
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);
  readonly task = this.store.selectSignal(selectRequiredDetailTask);
  readonly users = this.store.selectSignal(selectAllUsers);

  selectAssignee(user: AppUser) {
    const task = this.task();
    if (!task) return;

    const assigneeSet = new Set(task.assignees.map((u) => u.id));

    if (assigneeSet.has(user.id)) {
      assigneeSet.delete(user.id);
    } else {
      assigneeSet.add(user.id);
    }

    const updated: UpdateProjectTaskRequest = {
      ...task,
      assigneeIds: Array.from(assigneeSet),
    };

    this.taskDetailService.updateTask(updated);
  }
}
