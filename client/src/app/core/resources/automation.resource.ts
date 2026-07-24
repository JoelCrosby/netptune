import { Signal } from '@angular/core';
import { netptunePermissions } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import { permissionResource } from './permission-resource';

export const automationRuleResource = <TRule>(
  ruleId: Signal<number | null>
) => {
  return permissionResource<ClientResponse<TRule>>(
    netptunePermissions.automations.read,
    () => {
      const id = ruleId();

      return id ? { url: `api/automations/${id}` } : undefined;
    }
  );
};
