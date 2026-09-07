import { PERMISSIONS } from '../auth/permissions';
import { AutomationBoardGroupOption } from '../models/automation-board-group-option';
import { permissionResource } from './permission.resource';

export const boardGroupOptionsResource = () => {
  return permissionResource<AutomationBoardGroupOption[]>({
    permission: PERMISSIONS.boardGroups.read,
    request: () => ({ url: 'api/boardgroups/options' }),
    defaultValue: [],
    refreshOn: ['boardGroups'],
  });
};
