import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { FlagResolutionType } from '@core/enums/flag-resolution-type';
import { AddBoardGroupRequest } from '@core/models/add-board-group-request';
import { ClientResponse } from '@core/models/client-response';
import { MoveTaskInGroupRequest } from '@core/models/move-task-in-group-request';
import { AddProjectTaskRequest, ProjectTask } from '@core/models/project-task';
import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { BulkUpdateTasksRequest } from '@core/models/requests/bulk-update-tasks-request';
import { DeleteTagFromTaskRequest } from '@core/models/requests/delete-tag-from-task-request';
import { MoveTasksToGroupRequest } from '@core/models/requests/move-tasks-to-group-request';
import { ReassignTasksRequest } from '@core/models/requests/re-assign-tasks-request';
import { UpdateBoardGroupRequest } from '@core/models/requests/update-board-group-request';
import { UpdateProjectTaskRequest } from '@core/models/requests/update-project-task-request';
import { Tag } from '@core/models/tag';
import { BoardGroupViewModel } from '@core/models/view-models/board-group-view-model';
import { BoardViewTask } from '@core/models/view-models/board-view';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { FileResponse } from '@core/types/file-response';
import { extractFilenameFromHeaders } from '@core/util/header-utils';
import { taskExportDefinition } from '@core/util/task-export-definition';
import { Observable, of, throwError } from 'rxjs';
import { switchMap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class TasksService {
  private http = inject(HttpClient);

  moveTaskInBoardGroup(request: MoveTaskInGroupRequest) {
    return this.http.post<ClientResponse>(
      'api/tasks/move-task-in-group',
      request
    );
  }

  post(task: AddProjectTaskRequest) {
    return this.http.post<ClientResponse<TaskViewModel>>('api/tasks', task);
  }

  put(task: ProjectTask | BoardViewTask | Partial<UpdateProjectTaskRequest>) {
    return this.http.put<ClientResponse<TaskViewModel>>('api/tasks', task);
  }

  bulkUpdate(request: BulkUpdateTasksRequest) {
    return this.http.post<ClientResponse>('api/tasks/bulk-update', request);
  }

  putGroup(request: UpdateBoardGroupRequest) {
    return this.http.put<ClientResponse<BoardGroupViewModel>>(
      'api/boardgroups',
      request
    );
  }

  delete(task: ProjectTask) {
    if (task.id === undefined || task.id === null) {
      throw new Error('task id undefined');
    }

    return this.http.delete<ClientResponse>(`api/tasks/${task.id}`);
  }

  deleteMultiple(ids: number[]) {
    return this.http.delete<ClientResponse>('api/tasks', { body: ids });
  }

  addTagToTask(request: AddTagToTaskRequest) {
    return this.http.post<ClientResponse<Tag>>('api/tags/task', request);
  }

  deleteTagFromTask(request: DeleteTagFromTaskRequest) {
    return this.http.delete<ClientResponse>('api/tags/task', { body: request });
  }

  addBoardGroup(request: AddBoardGroupRequest) {
    return this.http.post<ClientResponse<BoardGroupViewModel>>(
      'api/boardgroups',
      request
    );
  }

  deleteBoardGroup(boardGroupId: number) {
    return this.http.delete<ClientResponse>(`api/boardgroups/${boardGroupId}`);
  }

  moveTasksToGroup(request: MoveTasksToGroupRequest) {
    return this.http.post<ClientResponse>(
      'api/tasks/move-tasks-to-group',
      request
    );
  }

  reassignTasks(request: ReassignTasksRequest) {
    return this.http.post<ClientResponse>('api/tasks/reassign-tasks', request);
  }

  resolveFlag(taskId: number, flagId: number, resolution: FlagResolutionType) {
    return this.http.put<ClientResponse>(
      `api/tasks/${taskId}/flags/${flagId}/resolution`,
      { resolution }
    );
  }

  export(boardId?: string): Observable<FileResponse> {
    return this.http
      .post(
        'api/export/run',
        { definition: taskExportDefinition(boardId) },
        { observe: 'response', responseType: 'blob' }
      )
      .pipe(
        switchMap((response) => {
          if (response.body === null) {
            return throwError(() => new Error('repsone body was null'));
          }

          return of({
            file: response.body,
            filename: extractFilenameFromHeaders(response.headers),
          });
        })
      );
  }
}
