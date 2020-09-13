import { Injectable } from '@angular/core';
import { HubService } from '@core/hubs/hub.service';
import { hubAction } from '@core/hubs/hub.utils';
import { ClientResponse } from '@core/models/client-response';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { tap } from 'rxjs/operators';
import * as actions from './board-groups.actions';

@Injectable()
export class BoardGroupHubService {
  constructor(private hub: HubService) {}

  connect() {
    this.hub.connect([
      {
        method: 'moveTaskInBoardGroupReceived',
        callback: (request: MoveTaskInGroupRequest) => {
          console.log({ action: hubAction(actions.moveTaskInBoardGroup) });

          this.hub.dispatch(actions.moveTaskInBoardGroup({ request }));
        },
      },
    ]);
  }

  moveTaskInBoardGroup(request: MoveTaskInGroupRequest) {
    return this.hub.invoke<ClientResponse>('MoveTaskInBoardGroup', request);
  }

  post(task: AddProjectTaskRequest) {
    return this.hub
      .invoke<TaskViewModel>('Create', task)
      .pipe(tap((response) => console.log({ response })));
  }
}
