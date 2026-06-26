import { inject, Injectable } from '@angular/core';
import { UpdateProjectTaskRequest } from '@app/core/models/requests/update-project-task-request';
import { selectCurrentHubGroupId } from '@app/core/store/hub-context/hub-context.selectors';
import {
  deleteProjectTask,
  editProjectTask,
  loadTaskDetails,
} from '@app/core/store/tasks/tasks.actions';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { SprintsService } from '@core/store/sprints/sprints.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { Store } from '@ngrx/store';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { catchError, EMPTY, tap } from 'rxjs';

@Injectable()
export class TaskDetailService {
  readonly store = inject(Store);
  readonly sprintsService = inject(SprintsService);
  readonly snackbar = inject(SnackbarService);
  readonly task = this.store.selectSignal(selectDetailTask);
  readonly hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);

  updateTask(update: Partial<UpdateProjectTaskRequest>) {
    const identifier = this.hubGroupId();
    const task = this.task();

    if (!identifier || !task) return;

    this.store.dispatch(
      editProjectTask.init({
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

    this.store.dispatch(deleteProjectTask.init({ identifier, task }));
  }

  assignSprint(sprintId: number) {
    const task = this.task();

    if (!task?.id) return;

    this.sprintsService
      .addTasks(sprintId, { taskIds: [task.id] })
      .pipe(
        unwrapClientReposne(),
        tap(() => {
          this.snackbar.open('Task added to sprint');
          this.reloadTaskDetail();
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }

  clearSprint() {
    const task = this.task();

    if (!task?.id || !task.sprintId) return;

    this.sprintsService
      .removeTask(task.sprintId, task.id)
      .pipe(
        unwrapClientReposne(),
        tap(() => {
          this.snackbar.open('Task removed from sprint');
          this.reloadTaskDetail();
        }),
        catchError(() => EMPTY)
      )
      .subscribe();
  }

  private reloadTaskDetail() {
    const task = this.task();

    if (!task?.systemId) return;

    this.store.dispatch(loadTaskDetails.init({ systemId: task.systemId }));
  }
}
