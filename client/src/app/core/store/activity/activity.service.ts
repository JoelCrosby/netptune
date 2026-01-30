import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { EntityType } from '@core/models/entity-type';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';

@Injectable({ providedIn: 'root' })
export class ActivityService {
  private http = inject(HttpClient);

  get(entityType: EntityType, entityId: number) {
    return this.http.get<ClientResponse<ActivityViewModel[]>>(
      `api/activity/${entityType}/${entityId}`
    );
  }
}
