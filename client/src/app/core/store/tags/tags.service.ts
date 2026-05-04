import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { appendPageParams, MAX_PAGE_SIZE } from '@core/models/pagination';
import {
  AddTagRequest,
  AddTagToTaskRequest,
} from '@core/models/requests/add-tag-request';
import { DeleteTagFromTaskRequest } from '@core/models/requests/delete-tag-from-task-request';
import { UpdateTagRequest } from '@core/models/requests/update-tag-request';
import { Tag } from '@core/models/tag';
import { DeleteTagsRequest } from '../../models/requests/delete-tag-request';

@Injectable({
  providedIn: 'root',
})
export class TagsService {
  private http = inject(HttpClient);

  get() {
    return this.http.get<Tag[]>('api/tags/workspace', {
      params: appendPageParams(new HttpParams(), { pageSize: MAX_PAGE_SIZE }),
    });
  }

  getForTask(systemId: string) {
    return this.http.get<Tag[]>(`api/tags/task/${systemId}`);
  }

  post(request: AddTagRequest) {
    return this.http.post<ClientResponse<Tag>>(`api/tags`, request);
  }

  postToTask(request: AddTagToTaskRequest) {
    return this.http.post<ClientResponse<Tag>>(`api/tags/task`, request);
  }

  delete(request: DeleteTagsRequest) {
    return this.http.request<ClientResponse>('DELETE', `api/tags`, {
      body: request,
    });
  }

  deleteFromTask(request: DeleteTagFromTaskRequest) {
    return this.http.request<ClientResponse>('DELETE', `api/tags/task`, {
      body: request,
    });
  }

  patch(request: UpdateTagRequest) {
    return this.http.patch<ClientResponse<Tag>>(`api/tags`, request);
  }
}
