import { netptunePermissions } from '../auth/permissions';
import { AutomationBoardGroupOption } from '../models/automation-board-group-option';
import { permissionResource } from './permission-resource';

export const boardGroupOptionsResource = () => {
  return permissionResource<AutomationBoardGroupOption[]>(
    netptunePermissions.boardGroups.read,
    () => ({ url: 'api/boardgroups/options' }),
    { defaultValue: [] }
  );
};
