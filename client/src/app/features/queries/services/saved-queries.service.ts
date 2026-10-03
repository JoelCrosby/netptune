import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { SaveQueryRequest, SavedQuery } from '../models/saved-query.models';

@Service()
export class SavedQueriesService {
  private http = inject(HttpClient);

  get(slug: string) {
    return this.http
      .get<ClientResponse<SavedQuery>>(`api/task-views/${slug}`)
      .pipe(unwrapClientResponse());
  }

  create(request: SaveQueryRequest) {
    return this.http
      .post<ClientResponse<SavedQuery>>('api/task-views', request)
      .pipe(unwrapClientResponse());
  }

  update(request: SaveQueryRequest) {
    return this.http
      .put<ClientResponse<SavedQuery>>('api/task-views', request)
      .pipe(unwrapClientResponse());
  }

  delete(slug: string) {
    return this.http.delete<ClientResponse>(`api/task-views/${slug}`);
  }
}
