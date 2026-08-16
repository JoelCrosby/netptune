import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import {
  AuditActivityPoint,
  AuditLogFilter,
} from '../models/view-models/audit-log-view-model';
import { permissionResource } from './permission.resource';

export const auditFilterParams = (filter: AuditLogFilter) => {
  const params: Record<string, string | number> = {};

  if (filter.userId) params['userId'] = filter.userId;
  if (filter.entityType !== undefined) params['entityType'] = filter.entityType;

  if (filter.activityType !== undefined) {
    params['activityType'] = filter.activityType;
  }

  if (filter.from) params['from'] = filter.from;
  if (filter.to) params['to'] = filter.to;

  return params;
};

export const auditSummaryResource = (filter: Signal<AuditLogFilter>) => {
  return permissionResource<AuditActivityPoint[]>(
    PERMISSIONS.audit.read,
    () => ({ url: 'api/audit/summary', params: auditFilterParams(filter()) }),
    {
      defaultValue: [],
      parse: (response) => {
        return (response as ClientResponse<AuditActivityPoint[]>).payload ?? [];
      },
    }
  );
};
