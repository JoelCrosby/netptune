import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  CreateTaskPinRequest,
  ReorderTaskPinsRequest,
  TaskPin,
} from '@core/models/task-pin';
import { unwrapClientResponse } from '@core/util/rxjs-operators';

@Service()
export class TaskPinsService {
  private http = inject(HttpClient);

  create(request: CreateTaskPinRequest) {
    return this.http
      .post<ClientResponse<TaskPin>>('api/pins', request)
      .pipe(unwrapClientResponse());
  }

  delete(id: number) {
    return this.http.delete<ClientResponse>(`api/pins/${id}`);
  }

  reorder(request: ReorderTaskPinsRequest) {
    return this.http.put<ClientResponse>('api/pins/reorder', request);
  }
}
