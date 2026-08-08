import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { AddBoardGroupRequest } from '@core/models/add-board-group-request';
import { ClientResponse } from '@core/models/client-response';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest, ProjectTask } from '@core/models/project-task';
import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { DeleteTagFromTaskRequest } from '@core/models/requests/delete-tag-from-task-request';
import { MoveTasksToGroupRequest } from '@core/models/requests/move-tasks-to-group-request';
import { ReassignTasksRequest } from '@core/models/requests/re-assign-tasks-request';
import { BulkUpdateTasksRequest } from '@core/models/requests/bulk-update-tasks-request';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { UpdateProjectTaskRequest } from '@core/models/requests/update-project-task-request';
import { Tag } from '@core/models/tag';
import { BoardGroupViewModel } from '@core/models/view-models/board-group-view-model';
import { BoardViewTask } from '@core/models/view-models/board-view';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { setCurrentGroupId } from '@core/store/hub-context/hub-context.actions';
import { WorkspaceEventsService } from '@core/sse/workspace-events.service';
import { Store } from '@ngrx/store';

@Injectable({
  providedIn: 'root',
})
export class ProjectTasksHubService {
  private http = inject(HttpClient);
  private store = inject(Store);
  private workspaceEvents = inject(WorkspaceEventsService);
  private workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly localReloads = signal(0);

  /** Task lists reload for a write made here and for one that arrived from elsewhere. */
  readonly updateVersion = computed(() => {
    return this.localReloads() + this.workspaceRefresh.version('tasks')();
  });

  addToGroup(groupId: string) {
    this.store.dispatch(setCurrentGroupId({ groupId }));
    this.workspaceEvents.joinGroup(groupId);
  }

  leaveGroup() {
    this.store.dispatch(setCurrentGroupId({ groupId: null }));
    this.workspaceEvents.leaveGroup();
  }

  reloadTaskList() {
    this.localReloads.update((version) => version + 1);
  }

  moveTaskInBoardGroup(
    boardIdentifier: string,
    request: MoveTaskInGroupRequest
  ) {
    return this.http.post<ClientResponse>(
      'api/tasks/move-task-in-group',
      request,
      { headers: { 'X-Group': boardIdentifier } }
    );
  }

  post(groupId: string, task: AddProjectTaskRequest) {
    return this.http.post<ClientResponse<TaskViewModel>>('api/tasks', task, {
      headers: { 'X-Group': groupId },
    });
  }

  put(
    groupId: string,
    task: ProjectTask | BoardViewTask | Partial<UpdateProjectTaskRequest>
  ) {
    return this.http.put<ClientResponse<TaskViewModel>>('api/tasks', task, {
      headers: { 'X-Group': groupId },
    });
  }

  bulkUpdate(groupId: string, request: BulkUpdateTasksRequest) {
    return this.http.post<ClientResponse>('api/tasks/bulk-update', request, {
      headers: { 'X-Group': groupId },
    });
  }

  putGroup(groupId: string, request: UpdateBoardGroupRequest) {
    return this.http.put<ClientResponse<BoardGroupViewModel>>(
      'api/boardgroups',
      request,
      { headers: { 'X-Group': groupId } }
    );
  }

  delete(groupId: string, task: ProjectTask) {
    if (task.id === undefined || task.id === null) {
      throw new Error('task id undefined');
    }

    return this.http.delete<ClientResponse>(`api/tasks/${task.id}`, {
      headers: { 'X-Group': groupId },
    });
  }

  deleteMultiple(groupId: string, ids: number[]) {
    return this.http.delete<ClientResponse>('api/tasks', {
      headers: { 'X-Group': groupId },
      body: ids,
    });
  }

  addTagToTask(groupId: string, request: AddTagToTaskRequest) {
    return this.http.post<ClientResponse<Tag>>('api/tags/task', request, {
      headers: { 'X-Group': groupId },
    });
  }

  deleteTagFromTask(groupId: string, request: DeleteTagFromTaskRequest) {
    return this.http.delete<ClientResponse>('api/tags/task', {
      headers: { 'X-Group': groupId },
      body: request,
    });
  }

  addBoardGroup(groupId: string, request: AddBoardGroupRequest) {
    return this.http.post<ClientResponse<BoardGroupViewModel>>(
      'api/boardgroups',
      request,
      { headers: { 'X-Group': groupId } }
    );
  }

  deleteBoardGroup(groupId: string, boardGroupId: number) {
    return this.http.delete<ClientResponse>(`api/boardgroups/${boardGroupId}`, {
      headers: { 'X-Group': groupId },
    });
  }

  moveTasksToGroup(groupId: string, request: MoveTasksToGroupRequest) {
    return this.http.post<ClientResponse>(
      'api/tasks/move-tasks-to-group',
      request,
      { headers: { 'X-Group': groupId } }
    );
  }

  reassignTasks(groupId: string, request: ReassignTasksRequest) {
    return this.http.post<ClientResponse>('api/tasks/reassign-tasks', request, {
      headers: { 'X-Group': groupId },
    });
  }
}
