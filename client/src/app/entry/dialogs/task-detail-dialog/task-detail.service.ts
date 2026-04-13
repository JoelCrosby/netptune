import { inject, Injectable } from '@angular/core';
import { UpdateProjectTaskRequest } from '@app/core/models/requests/update-project-task-request';
import { selectCurrentHubGroupId } from '@app/core/store/hub-context/hub-context.selectors';
import {
  deleteProjectTask,
  editProjectTask,
} from '@app/core/store/tasks/tasks.actions';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { Store } from '@ngrx/store';

@Injectable({
  providedIn: 'root',
})
export class TaskDetailService {
  readonly store = inject(Store);
  readonly task = this.store.selectSignal(selectDetailTask);
  readonly hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);

  updateTask(update: Partial<UpdateProjectTaskRequest>) {
    const identifier = this.hubGroupId();
    const task = this.task();

    if (!identifier || !task) return;

    this.store.dispatch(
      editProjectTask({
        identifier,
        task: {
          ...task,
          ...update,
        },
      })
    );
  }

  deleteTask() {
    const task = this.task();
    const identifier = this.hubGroupId();

    if (!task || !identifier) return;

    this.store.dispatch(deleteProjectTask({ identifier, task }));
  }
}
