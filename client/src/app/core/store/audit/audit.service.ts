import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
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

  private buildParams(filter: AuditLogFilter): Record<string, string> {
    const params: Record<string, string> = {};

    if (filter.userId) params['userId'] = filter.userId;
    if (filter.entityType !== undefined)
      params['entityType'] = String(filter.entityType);
    if (filter.activityType !== undefined)
      params['activityType'] = String(filter.activityType);
    if (filter.from) params['from'] = filter.from;
    if (filter.to) params['to'] = filter.to;
    if (filter.page !== undefined) params['page'] = String(filter.page);
    if (filter.pageSize !== undefined)
      params['pageSize'] = String(filter.pageSize);

    return params;
  }
}
