import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { AddTagRequest } from '@core/models/requests/add-tag-request';
import { DeleteTagFromTaskRequest } from '@core/models/requests/delete-tag-from-task-request';
import { Tag } from '@core/models/tag';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root',
})
export class TagsService {
  constructor(private http: HttpClient) {}

  get(workspaceSlug: string) {
    return this.http.get<Tag[]>(
      environment.apiEndpoint + `api/tags/workspace/${workspaceSlug}`
    );
  }

  getForTask(systemId: string, workspaceSlug: string) {
    return this.http.get<Tag[]>(
      environment.apiEndpoint + `api/tags/task/${systemId}`,
      {
        params: { workspaceSlug },
      }
    );
  }

  post(request: AddTagRequest) {
    return this.http.post<ClientResponse<Tag>>(
      environment.apiEndpoint + `api/tags/task`,
      request
    );
  }

  delete(request: AddTagRequest) {
    return this.http.request<ClientResponse>(
      'DELETE',
      environment.apiEndpoint + `api/tags/task`,
      {
        body: request,
      }
    );
  }

  deleteFromTask(request: DeleteTagFromTaskRequest) {
    return this.http.request<ClientResponse>(
      'DELETE',
      environment.apiEndpoint + `api/tags/task`,
      {
        body: request,
      }
    );
  }
}
