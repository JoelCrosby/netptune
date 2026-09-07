import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import { permissionResource, requestFrom } from './permission.resource';

export const automationRuleResource = <TRule>(
  ruleId: Signal<number | null>
) => {
  return permissionResource<ClientResponse<TRule>>({
    permission: PERMISSIONS.automations.read,
    request: requestFrom(ruleId, (id) => ({ url: `api/automations/${id}` })),
  });
};
