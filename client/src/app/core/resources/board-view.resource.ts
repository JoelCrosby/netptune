import { Signal } from '@angular/core';
import { Params } from '@angular/router';
import { requestFrom } from './permission.resource';
import { PERMISSIONS } from '../auth/permissions';
import { BoardView } from '../models/view-models/board-view';
import { stableResource } from './stable.resource';

export const boardViewResource = (
  identifier: Signal<string | undefined>,
  params: Signal<Params>
) => {
  return stableResource<BoardView | undefined>({
    permission: PERMISSIONS.boards.read,
    request: requestFrom(identifier, (id) => ({
      url: `api/boards/view/${id}`,
      params: params(),
    })),
    refreshOn: ['tasks', 'boardGroups', 'pins'],
    parse: (response) => response.payload,
  });
};
