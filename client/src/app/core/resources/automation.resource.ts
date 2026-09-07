import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import { permissionResource } from './permission.resource';

export const automationRuleResource = <TRule>(
  ruleId: Signal<number | null>
) => {
  return permissionResource<ClientResponse<TRule>>({
    permission: PERMISSIONS.automations.read,
    request: () => {
      const id = ruleId();

      return id ? { url: `api/automations/${id}` } : undefined;
    },
  });
};
