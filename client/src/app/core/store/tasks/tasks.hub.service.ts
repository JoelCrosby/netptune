import { Injectable } from '@angular/core';
import * as groupsActions from '@boards/store/groups/board-groups.actions';
import { HubService } from '@core/hubs/hub.service';
import { AddBoardGroupRequest } from '@core/models/add-board-group-request';
import { ClientResponse } from '@core/models/client-response';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest, ProjectTask } from '@core/models/project-task';
import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { DeleteTagFromTaskRequest } from '@core/models/requests/delete-tag-from-task-request';
import { MoveTasksToGroupRequest } from '@core/models/requests/move-tasks-to-group-request';
import { ReassignTasksRequest } from '@core/models/requests/re-assign-tasks-request';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { Tag } from '@core/models/tag';
import { UserConnection } from '@core/models/user-connection';
import { BoardGroupViewModel } from '@core/models/view-models/board-group-view-model';
import { BoardViewTask } from '@core/models/view-models/board-view';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { setCurrentGroupId } from '@core/store/hub-context/hub-context.actions';
import { Store } from '@ngrx/store';
import { first, tap } from 'rxjs/operators';
import { selectIsWorkspaceGroup } from '../hub-context/hub-context.selectors';
import * as actions from './tasks.actions';

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
        method: 'UpdateGroup',
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
      {
        method: 'AddBoardGroup',
        callback: () => this.reloadRequiredViews(),
      },
      {
        method: 'DeleteBoardGroup',
        callback: () => this.reloadRequiredViews(),
      },
      {
        method: 'MoveTasksToGroup',
        callback: () => this.reloadRequiredViews(),
      },
      {
        method: 'ReassignTasks',
        callback: () => this.reloadRequiredViews(),
      },
    ]);
  }

  reloadRequiredViews() {
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
    return this.hub.invoke<ClientResponse<TaskViewModel>>(
      'create',
      groupId,
      task
    );
  }

  put(groupId: string, task: ProjectTask | BoardViewTask) {
    return this.hub.invoke<ClientResponse<TaskViewModel>>(
      'update',
      groupId,
      task
    );
  }

  putGroup(groupId: string, request: UpdateBoardGroupRequest) {
    return this.hub.invoke<ClientResponse<BoardGroupViewModel>>(
      'updateGroup',
      groupId,
      request
    );
  }

  delete(groupId: string, task: ProjectTask) {
    return this.hub.invoke<ClientResponse>('Delete', groupId, task.id);
  }

  deleteMultiple(groupId: string, ids: number[]) {
    return this.hub.invoke<ClientResponse>('DeleteMultiple', groupId, ids);
  }

  addTagToTask(groupId: string, request: AddTagToTaskRequest) {
    return this.hub.invoke<ClientResponse<Tag>>(
      'AddTagToTask',
      groupId,
      request
    );
  }

  deleteTagFromTask(groupId: string, request: DeleteTagFromTaskRequest) {
    return this.hub.invoke<ClientResponse>(
      'DeleteTagFromTask',
      groupId,
      request
    );
  }

  addBoardGroup(groupId: string, request: AddBoardGroupRequest) {
    return this.hub.invoke<ClientResponse<BoardGroupViewModel>>(
      'AddBoardGroup',
      groupId,
      request
    );
  }

  deleteBoardGroup(groupId: string, boardGroupId: number) {
    return this.hub.invoke<ClientResponse>(
      'DeleteBoardGroup',
      groupId,
      boardGroupId
    );
  }

  moveTasksToGroup(groupId: string, request: MoveTasksToGroupRequest) {
    return this.hub.invoke<ClientResponse>(
      'MoveTasksToGroup',
      groupId,
      request
    );
  }

  reassignTasks(groupId: string, request: ReassignTasksRequest) {
    return this.hub.invoke<ClientResponse>('ReassignTasks', groupId, request);
  }
}
