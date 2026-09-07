import { PERMISSIONS } from '../auth/permissions';
import { ServiceAccount } from '../models/service-account';
import { permissionResource } from './permission.resource';

export const serviceAccountResource = () => {
  return permissionResource<ServiceAccount[]>({
    permission: PERMISSIONS.serviceAccounts.read,
    request: () => ({ url: 'api/service-accounts' }),
    defaultValue: [],
  });
};
