import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  CreateTaskRelationRequest,
  TaskRelation,
} from '@core/models/task-relation';

@Injectable({
  providedIn: 'root',
})
export class TaskRelationsService {
  private http = inject(HttpClient);

  get(systemId: string) {
    return this.http.get<TaskRelation[]>(`api/task-relations/${systemId}`);
  }

  create(request: CreateTaskRelationRequest, groupId?: string | null) {
    return this.http.post<ClientResponse<TaskRelation>>(
      'api/task-relations',
      request,
      { headers: groupId ? { 'X-Group': groupId } : {} }
    );
  }

  delete(id: number, groupId?: string | null) {
    return this.http.delete<ClientResponse>(`api/task-relations/${id}`, {
      headers: groupId ? { 'X-Group': groupId } : {},
    });
  }
}
