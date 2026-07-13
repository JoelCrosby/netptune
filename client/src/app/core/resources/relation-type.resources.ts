import { netptunePermissions } from '../auth/permissions';
import { RelationType } from '../models/relation-type';
import { permissionResource } from './permission-resource';

export const relationTypeResource = () => {
  return permissionResource<RelationType[]>(
    netptunePermissions.relationTypes.read,
    () => ({ url: 'api/relation-types' }),
    { defaultValue: [] }
  );
};
