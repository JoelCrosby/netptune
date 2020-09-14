import { Injectable } from '@angular/core';
import * as groupsActions from '@boards/store/groups/board-groups.actions';
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

  async connect(boardIdentifier: string) {
    await this.hub.connect('board-hub', [
      {
        method: 'Create',
        callback: () => {
          this.hub.dispatch(actions.loadProjectTasks(), false);
          this.hub.dispatch(groupsActions.loadBoardGroups(), false);
        },
      },
      {
        method: 'Delete',
        callback: () => {
          this.hub.dispatch(actions.loadProjectTasks(), false);
          this.hub.dispatch(groupsActions.loadBoardGroups(), false);
        },
      },
      {
        method: 'Update',
        callback: () => {
          this.hub.dispatch(actions.loadProjectTasks(), false);
          this.hub.dispatch(groupsActions.loadBoardGroups(), false);
        },
      },
    ]);

    this.addToBoard(boardIdentifier);
  }

  disconnect() {
    this.hub.disconnect();
  }

  addToBoard(boardIdentifier: string) {
    return this.hub.invoke<string>('AddToBoard', boardIdentifier);
  }

  removeFromBoard(boardIdentifier: string) {
    return this.hub.invoke<string>('RemoveFromBoard', boardIdentifier);
  }

  post(boardIdentifier: string, task: AddProjectTaskRequest) {
    return this.hub.invoke<TaskViewModel>('Create', boardIdentifier, task);
  }

  put(boardIdentifier: string, task: ProjectTask) {
    return this.hub.invoke<TaskViewModel>('update', boardIdentifier, task);
  }

  delete(boardIdentifier: string, task: ProjectTask) {
    return this.hub.invoke<ClientResponse>('Delete', boardIdentifier, task.id);
  }
}
