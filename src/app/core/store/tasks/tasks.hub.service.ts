import { Injectable } from '@angular/core';
import { HubService } from '@core/hubs/hub.service';
import { ClientResponse } from '@core/models/client-response';
import { AddProjectTaskRequest, ProjectTask } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import * as actions from './tasks.actions';

@Injectable({
  providedIn: 'root',
})
export class ProjectTasksHubService {
  constructor(private hub: HubService) {}

  connect() {
    this.hub.connect([
      {
        method: 'createReceived',
        callback: (task: TaskViewModel) => {
          this.hub.dispatch(actions.createProjectTasksSuccess({ task }));
        },
      },
      {
        method: 'deleteReceived',
        callback: (response: ClientResponse, taskId: number) => {
          this.hub.dispatch(
            actions.deleteProjectTasksSuccess({ response, taskId })
          );
        },
      },
      {
        method: 'updateReceived',
        callback: (task: TaskViewModel) => {
          this.hub.dispatch(actions.editProjectTasksSuccess({ task }));
        },
      },
    ]);
  }

  post(task: AddProjectTaskRequest) {
    return this.hub.invoke<TaskViewModel>('Create', task);
  }

  put(task: ProjectTask) {
    return this.hub.invoke<TaskViewModel>('update', task);
  }

  delete(task: ProjectTask) {
    return this.hub.invoke<ClientResponse>('Delete', task);
  }
}
