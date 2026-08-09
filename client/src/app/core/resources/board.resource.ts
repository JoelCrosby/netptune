import { MAX_PAGE_SIZE } from '../models/pagination';
import { BoardsViewModel } from '../models/view-models/boards-view-model';
import { PERMISSONS } from '../auth/permissions';
import { permissionResource } from './permission-resource';

export const workspaceBoardsResource = () => {
  return permissionResource<BoardsViewModel[]>(
    PERMISSONS.boards.read,
    () => ({
      url: 'api/boards/workspace',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    }),
    { defaultValue: [], refreshOn: ['boards'] }
  );
};
