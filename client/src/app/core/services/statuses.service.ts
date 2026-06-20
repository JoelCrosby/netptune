import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { EntityType } from '@core/models/entity-type';
import {
  CreateStatusRequest,
  ReorderStatusesRequest,
  Status,
  UpdateStatusRequest,
} from '@core/models/status';

@Injectable({
  providedIn: 'root',
})
export class StatusesService {
  private http = inject(HttpClient);

  get(entityType = EntityType.task) {
    return this.http.get<Status[]>('api/statuses', {
      params: new HttpParams().set('entityType', entityType),
    });
  }

  create(request: CreateStatusRequest) {
    return this.http.post<ClientResponse<Status>>('api/statuses', request);
  }

  update(request: UpdateStatusRequest) {
    return this.http.put<ClientResponse<Status>>('api/statuses', request);
  }

  delete(id: number) {
    return this.http.delete<ClientResponse>(`api/statuses/${id}`);
  }

  reorder(request: ReorderStatusesRequest) {
    return this.http.post<ClientResponse>('api/statuses/reorder', request);
  }
}
