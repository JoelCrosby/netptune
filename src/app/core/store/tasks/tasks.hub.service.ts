import { Injectable } from '@angular/core';
import * as groupsActions from '@boards/store/groups/board-groups.actions';
import { HubService } from '@core/hubs/hub.service';
import { ClientResponse } from '@core/models/client-response';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest, ProjectTask } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { tap } from 'rxjs/operators';
import * as actions from './tasks.actions';

@Injectable({
  providedIn: 'root',
})
export class ProjectTasksHubService {
  constructor(private hub: HubService) {}

  async connect(boardIdentifier: string) {
    await this.hub.connect('board-hub', [
      {
        method: 'MoveTaskInBoardGroup',
        callback: () => {
          this.hub.dispatch(actions.loadProjectTasks());
          this.hub.dispatch(groupsActions.loadBoardGroups());
        },
      },
      {
        method: 'Create',
        callback: () => {
          this.hub.dispatch(actions.loadProjectTasks());
          this.hub.dispatch(groupsActions.loadBoardGroups());
        },
      },
      {
        method: 'Delete',
        callback: () => {
          this.hub.dispatch(actions.loadProjectTasks());
          this.hub.dispatch(groupsActions.loadBoardGroups());
        },
      },
      {
        method: 'Update',
        callback: () => {
          this.hub.dispatch(actions.loadProjectTasks());
          this.hub.dispatch(groupsActions.loadBoardGroups());
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

  moveTaskInBoardGroup(
    boardIdentifier: string,
    request: MoveTaskInGroupRequest
  ) {
    return this.hub.invoke<ClientResponse>(
      'MoveTaskInBoardGroup',
      boardIdentifier,
      request
    );
  }

  post(boardIdentifier: string, task: AddProjectTaskRequest) {
    return this.hub.invoke<TaskViewModel>('Create', boardIdentifier, task);
  }

  put(boardIdentifier: string, task: ProjectTask) {
    return this.hub.invoke<TaskViewModel>('update', boardIdentifier, task);
  }

  delete(boardIdentifier: string, task: ProjectTask) {
    return this.hub
      .invoke<ClientResponse>('Delete', boardIdentifier, task.id)
      .pipe(tap((res) => console.log({ res })));
  }
}
