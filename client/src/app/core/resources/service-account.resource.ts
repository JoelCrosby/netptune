import { PERMISSIONS } from '../auth/permissions';
import { ServiceAccount } from '../models/service-account';
import { permissionResource } from './permission.resource';

export const serviceAccountResource = () => {
  return permissionResource<ServiceAccount[]>(
    PERMISSIONS.serviceAccounts.read,
    () => ({ url: 'api/service-accounts' }),
    { defaultValue: [] }
  );
};
