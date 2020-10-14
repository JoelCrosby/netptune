import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { CommentViewModel } from '@core/models/comment';
import { AddProjectTaskRequest, ProjectTask } from '@core/models/project-task';
import { AddCommentRequest } from '@core/models/requests/add-comment-request';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { FileResponse } from '@core/types/file-response';
import { extractFilenameFromHeaders } from '@core/util/header-utils';
import { environment } from '@env/environment';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class ProjectTasksService {
  constructor(private http: HttpClient) {}

  get(workspaceSlug: string) {
    return this.http.get<TaskViewModel[]>(
      environment.apiEndpoint + `api/tasks?workspaceSlug=${workspaceSlug}`
    );
  }

  post(task: AddProjectTaskRequest) {
    return this.http.post<ClientResponse<TaskViewModel>>(
      environment.apiEndpoint + `api/tasks`,
      task
    );
  }

  put(task: ProjectTask) {
    return this.http.put<ClientResponse<TaskViewModel>>(
      environment.apiEndpoint + `api/tasks`,
      task
    );
  }

  delete(task: ProjectTask) {
    return this.http.delete<ClientResponse>(
      environment.apiEndpoint + `api/tasks/${task.id}`
    );
  }

  detail(systemId: string, workspaceSlug: string) {
    return this.http.get<TaskViewModel>(
      environment.apiEndpoint + 'api/tasks/detail',
      {
        params: {
          workspace: workspaceSlug,
          systemId,
        },
      }
    );
  }

  postComment(request: AddCommentRequest) {
    return this.http.post<CommentViewModel>(
      environment.apiEndpoint + 'api/comments/task',
      request
    );
  }

  getComments(systemId: string, workspaceSlug: string) {
    return this.http.get<CommentViewModel[]>(
      environment.apiEndpoint + `api/comments/task/${systemId}`,
      {
        params: {
          workspace: workspaceSlug,
        },
      }
    );
  }

  deleteComment(commentId: number) {
    return this.http.delete<ClientResponse>(
      environment.apiEndpoint + `api/comments/${commentId}`
    );
  }

  export(workspaceSlug: string): Observable<FileResponse> {
    return this.http
      .get(
        environment.apiEndpoint + `api/export/tasks/export-workspace/${workspaceSlug}`,
        {
          observe: 'response',
          responseType: 'blob',
        }
      )
      .pipe(
        map((response) => ({
          file: response.body,
          filename: extractFilenameFromHeaders(response.headers),
        }))
      );
  }

  import(boardIdentifier: string, file: File): Observable<ClientResponse> {
    const formData = new FormData();
    formData.append('files', file);

    return this.http.post<ClientResponse>(
      environment.apiEndpoint + `api/import/tasks/${boardIdentifier}`,
      formData
    );
  }
}
