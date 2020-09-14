import { Injectable } from '@angular/core';
import { HubService } from '@core/hubs/hub.service';
import { ClientResponse } from '@core/models/client-response';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { tap } from 'rxjs/operators';
import * as actions from './board-groups.actions';

@Injectable()
export class BoardGroupHubService {
  constructor(private hub: HubService) {}

  async connect(boardIdentifier: string) {
    await this.hub.connect('board-hub', [
      {
        method: 'MoveTaskInBoardGroup',
        callback: (request: MoveTaskInGroupRequest) =>
          this.hub.dispatch(actions.moveTaskInBoardGroup({ request })),
      },
    ]);

    this.addToBoard(boardIdentifier);
  }

  disconnect() {
    this.hub.disconnect();
  }

  removeFromBoard(boardIdentifier: string) {
    return this.hub.invoke<string>('RemoveFromBoard', boardIdentifier);
  }

  addToBoard(boardIdentifier: string) {
    return this.hub.invoke<string>('AddToBoard', boardIdentifier);
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
    return this.hub
      .invoke<TaskViewModel>('Create', boardIdentifier, task)
      .pipe(tap((response) => console.log({ response })));
  }
}
