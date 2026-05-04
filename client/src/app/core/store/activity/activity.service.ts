import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { EntityType } from '@core/models/entity-type';
import {
  appendCursorParams,
  CursorPage,
  cursorPageFromHeaders,
  CursorQuery,
  DEFAULT_PAGE_SIZE,
} from '@core/models/pagination';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class ActivityService {
  private http = inject(HttpClient);

  get(
    entityType: EntityType,
    entityId: number,
    query?: CursorQuery
  ) {
    const params = appendCursorParams(new HttpParams(), {
      take: query?.take ?? DEFAULT_PAGE_SIZE,
      cursor: query?.cursor,
    });

    return this.http
      .get<ClientResponse<ActivityViewModel[]>>(
        `api/activity/${entityType}/${entityId}`,
        { params, observe: 'response' }
      )
      .pipe(
        map((response): CursorPage<ActivityViewModel> => ({
          ...cursorPageFromHeaders(
            response.body?.payload ?? [],
            response.headers
          ),
        }))
      );
  }
}
