import { PERMISSIONS } from '../auth/permissions';
import { RelationType } from '../models/relation-type';
import { permissionResource } from './permission.resource';

export const relationTypeResource = () => {
  return permissionResource<RelationType[]>({
    permission: PERMISSIONS.relationTypes.read,
    request: () => ({ url: 'api/relation-types' }),
    defaultValue: [],
  });
};
