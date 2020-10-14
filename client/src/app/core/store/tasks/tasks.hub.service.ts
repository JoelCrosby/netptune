import { Injectable } from '@angular/core';
import * as groupsActions from '@boards/store/groups/board-groups.actions';
import { HubService } from '@core/hubs/hub.service';
import { ClientResponse } from '@core/models/client-response';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest, ProjectTask } from '@core/models/project-task';
import { UserConnection } from '@core/models/user-connection';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { first, tap } from 'rxjs/operators';
import { setCurrentGroupId } from '@core/store/hub-context/hub-context.actions';
import * as actions from './tasks.actions';
import { selectIsWorkspaceGroup } from '../hub-context/hub-context.selectors';
import { DeleteTagFromTaskRequest } from '@core/models/requests/delete-tag-from-task-request';
import { AddTagRequest } from '@core/models/requests/add-tag-request';
import { Tag } from '@core/models/tag';
import { BoardViewTask } from '@core/models/view-models/board-view';

@Injectable({
  providedIn: 'root',
})
export class ProjectTasksHubService {
  constructor(private hub: HubService, private store: Store) {}

  async connect() {
    await this.hub.connect('board-hub', [
      {
        method: 'MoveTaskInBoardGroup',
        callback: () => this.reloadRequiredViews(),
      },
      {
        method: 'Create',
        callback: () => this.reloadRequiredViews(),
      },
      {
        method: 'Delete',
        callback: () => this.reloadRequiredViews(),
      },
      {
        method: 'DeleteMultiple',
        callback: () => this.reloadRequiredViews(),
      },
      {
        method: 'Update',
        callback: () => this.reloadRequiredViews(),
      },
      {
        method: 'AddTagToTask',
        callback: () => this.reloadRequiredViews(),
      },
      {
        method: 'DeleteTagFromTask',
        callback: () => this.reloadRequiredViews(),
      },
    ]);
  }

  reloadRequiredViews() {
    console.log('REQUEST TO RELOAD RECIEVED');

    this.store
      .select(selectIsWorkspaceGroup)
      .pipe(
        first(),
        tap((res) =>
          res
            ? this.hub.dispatch(actions.loadProjectTasks())
            : this.hub.dispatch(groupsActions.loadBoardGroups())
        )
      )
      .subscribe();
  }

  disconnect() {
    this.hub.disconnect();
  }

  addToGroup(groupId: string) {
    this.store.dispatch(setCurrentGroupId({ groupId }));
    return this.hub.invoke<UserConnection>('AddToGroup', groupId);
  }

  removeFromGroup(groupId: string) {
    this.store.dispatch(setCurrentGroupId({ groupId: null }));
    return this.hub.invoke<UserConnection>('RemoveFromGroup', groupId);
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

  post(groupId: string, task: AddProjectTaskRequest) {
    return this.hub.invoke<TaskViewModel>('Create', groupId, task);
  }

  put(groupId: string, task: ProjectTask | BoardViewTask) {
    return this.hub.invoke<TaskViewModel>('update', groupId, task);
  }

  delete(groupId: string, task: ProjectTask) {
    return this.hub.invoke<ClientResponse>('Delete', groupId, task.id);
  }

  deleteMultiple(groupId: string, ids: number[]) {
    return this.hub.invoke<ClientResponse>('DeleteMultiple', groupId, ids);
  }

  addTagToTask(groupId: string, request: AddTagRequest) {
    return this.hub.invoke<ClientResponse<Tag>>('AddTagToTask', groupId, request);
  }

  deleteTagFromTask(groupId: string, request: DeleteTagFromTaskRequest) {
    return this.hub.invoke<ClientResponse>(
      'DeleteTagFromTask',
      groupId,
      request
    );
  }
}
