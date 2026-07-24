import { netptunePermissions } from '../auth/permissions';
import { ServiceAccount } from '../models/service-account';
import { permissionResource } from './permission-resource';

export const serviceAccountResource = () => {
  return permissionResource<ServiceAccount[]>(
    netptunePermissions.serviceAccounts.read,
    () => ({ url: 'api/service-accounts' }),
    { defaultValue: [] }
  );
};
