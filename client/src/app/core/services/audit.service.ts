import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { auditFilterParams } from '@core/resources/audit.resource';
import { AuditLogFilter } from '@core/models/view-models/audit-log-view-model';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private http = inject(HttpClient);

  exportAuditLog(filter: AuditLogFilter) {
    return this.http.get('api/audit/export', {
      params: new HttpParams({ fromObject: auditFilterParams(filter) }),
      responseType: 'blob',
      observe: 'response',
    });
  }
}
