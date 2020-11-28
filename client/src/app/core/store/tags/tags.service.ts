import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  AddTagRequest,
  AddTagToTaskRequest,
} from '@core/models/requests/add-tag-request';
import { DeleteTagFromTaskRequest } from '@core/models/requests/delete-tag-from-task-request';
import { UpdateTagRequest } from '@core/models/requests/update-tag-request';
import { Tag } from '@core/models/tag';
import { environment } from '@env/environment';
import { DeleteTagsRequest } from '../../models/requests/delete-tag-request';

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
      environment.apiEndpoint + `api/tags`,
      request
    );
  }

  postToTask(request: AddTagToTaskRequest) {
    return this.http.post<ClientResponse<Tag>>(
      environment.apiEndpoint + `api/tags/task`,
      request
    );
  }

  delete(request: DeleteTagsRequest) {
    return this.http.request<ClientResponse>(
      'DELETE',
      environment.apiEndpoint + `api/tags`,
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

  patch(request: UpdateTagRequest) {
    return this.http.patch<ClientResponse<Tag>>(
      environment.apiEndpoint + `api/tags`,
      request
    );
  }
}
