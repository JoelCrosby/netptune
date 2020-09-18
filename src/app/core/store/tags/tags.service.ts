import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AddTagRequest } from '@core/models/requests/add-tag-request';
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
    return this.http.post<Tag>(
      environment.apiEndpoint + `api/tags/task`,
      request
    );
  }

  delete(request: AddTagRequest) {
    return this.http.request<Tag>(
      'DELETE',
      environment.apiEndpoint + `api/tags/task`,
      {
        body: request,
      }
    );
  }
}
