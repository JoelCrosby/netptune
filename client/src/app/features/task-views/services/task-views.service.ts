import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { SaveTaskViewRequest, TaskView } from '../models/task-view.models';

@Service()
export class TaskViewsService {
  private http = inject(HttpClient);

  get(slug: string) {
    return this.http
      .get<ClientResponse<TaskView>>(`api/task-views/${slug}`)
      .pipe(unwrapClientResponse());
  }

  create(request: SaveTaskViewRequest) {
    return this.http
      .post<ClientResponse<TaskView>>('api/task-views', request)
      .pipe(unwrapClientResponse());
  }

  update(request: SaveTaskViewRequest) {
    return this.http
      .put<ClientResponse<TaskView>>('api/task-views', request)
      .pipe(unwrapClientResponse());
  }

  delete(slug: string) {
    return this.http.delete<ClientResponse>(`api/task-views/${slug}`);
  }
}
