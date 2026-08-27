import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  CreateRelationTypeRequest,
  MoveRelationTypeRequest,
  RelationType,
  ReorderRelationTypesRequest,
  UpdateRelationTypeRequest,
} from '@core/models/relation-type';

@Service()
export class RelationTypesService {
  private http = inject(HttpClient);

  get() {
    return this.http.get<RelationType[]>('api/relation-types');
  }

  create(request: CreateRelationTypeRequest) {
    return this.http.post<ClientResponse<RelationType>>(
      'api/relation-types',
      request
    );
  }

  update(request: UpdateRelationTypeRequest) {
    return this.http.put<ClientResponse<RelationType>>(
      'api/relation-types',
      request
    );
  }

  delete(id: number) {
    return this.http.delete<ClientResponse>(`api/relation-types/${id}`);
  }

  reorder(request: ReorderRelationTypesRequest) {
    return this.http.post<ClientResponse>(
      'api/relation-types/reorder',
      request
    );
  }

  move(request: MoveRelationTypeRequest) {
    return this.http.post<ClientResponse>('api/relation-types/move', request);
  }
}
