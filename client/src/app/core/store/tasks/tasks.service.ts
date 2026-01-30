import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { CommentViewModel } from '@core/models/comment';
import { AddProjectTaskRequest, ProjectTask } from '@core/models/project-task';
import { AddCommentRequest } from '@core/models/requests/add-comment-request';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { FileResponse } from '@core/types/file-response';
import { extractFilenameFromHeaders } from '@core/util/header-utils';
import { Observable, of, throwError } from 'rxjs';
import { switchMap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class ProjectTasksService {
  private http = inject(HttpClient);

  get() {
    return this.http.get<TaskViewModel[]>('api/tasks');
  }

  post(task: AddProjectTaskRequest) {
    return this.http.post<ClientResponse<TaskViewModel>>(`api/tasks`, task);
  }

  put(task: ProjectTask) {
    return this.http.put<ClientResponse<TaskViewModel>>(`api/tasks`, task);
  }

  delete(task: ProjectTask) {
    if (task.id === undefined || task.id === null) {
      throw new Error('task id undefined');
    }

    return this.http.delete<ClientResponse>(`api/tasks/${task.id}`);
  }

  detail(systemId: string) {
    return this.http.get<TaskViewModel>('api/tasks/detail', {
      params: {
        systemId,
      },
    });
  }

  postComment(request: AddCommentRequest) {
    return this.http.post<ClientResponse<CommentViewModel>>(
      'api/comments/task',
      request
    );
  }

  getComments(systemId: string) {
    return this.http.get<CommentViewModel[]>(`api/comments/task/${systemId}`);
  }

  deleteComment(commentId: number) {
    return this.http.delete<ClientResponse>(`api/comments/${commentId}`);
  }

  export(): Observable<FileResponse> {
    return this.http
      .get(`api/export/tasks/export-workspace`, {
        observe: 'response',
        responseType: 'blob',
      })
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

  import(boardIdentifier: string, file: File): Observable<ClientResponse> {
    const formData = new FormData();
    formData.append('files', file);

    return this.http.post<ClientResponse>(
      `api/import/tasks/${boardIdentifier}`,
      formData
    );
  }
}
