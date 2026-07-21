import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { appendPageParams } from '@core/models/pagination';
import {
  AuditActivityPoint,
  AuditLogFilter,
  AuditLogPage,
} from '@core/models/view-models/audit-log-view-model';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private http = inject(HttpClient);

  getAuditLog(filter: AuditLogFilter) {
    const params = this.buildParams(filter);
    return this.http.get<ClientResponse<AuditLogPage>>('api/audit', { params });
  }

  getActivitySummary(filter: AuditLogFilter) {
    const params = this.buildParams(filter);
    return this.http.get<ClientResponse<AuditActivityPoint[]>>(
      'api/audit/summary',
      { params }
    );
  }

  exportAuditLog(filter: AuditLogFilter) {
    const params = this.buildParams(filter);
    return this.http.get('api/audit/export', {
      params,
      responseType: 'blob',
      observe: 'response',
    });
  }

  private buildParams(filter: AuditLogFilter): HttpParams {
    let params = appendPageParams(new HttpParams(), filter);

    if (filter.userId) params = params.set('userId', filter.userId);
    if (filter.entityType !== undefined)
      params = params.set('entityType', filter.entityType);
    if (filter.activityType !== undefined)
      params = params.set('activityType', filter.activityType);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);

    return params;
  }
}
