import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  CreateTaskRelationRequest,
  TaskRelation,
} from '@core/models/task-relation';

@Service()
export class TaskRelationsService {
  private http = inject(HttpClient);

  get(systemId: string) {
    return this.http.get<TaskRelation[]>(`api/task-relations/${systemId}`);
  }

  create(request: CreateTaskRelationRequest) {
    return this.http.post<ClientResponse<TaskRelation>>(
      'api/task-relations',
      request
    );
  }

  delete(id: number) {
    return this.http.delete<ClientResponse>(`api/task-relations/${id}`);
  }
}
