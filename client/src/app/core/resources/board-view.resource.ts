import { Signal } from '@angular/core';
import { Params } from '@angular/router';
import { netptunePermissions } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import { BoardView } from '../models/view-models/board-view';
import { stableResource } from './stable-resource';

export const boardViewResource = (
  identifier: Signal<string | undefined>,
  params: Signal<Params>
) => {
  return stableResource<BoardView | undefined>(
    netptunePermissions.boards.read,
    () => {
      const id = identifier();

      if (!id) return undefined;

      return { url: `api/boards/view/${id}`, params: params() };
    },
    {
      refreshOn: ['tasks', 'boardGroups'],
      parse: (response) => (response as ClientResponse<BoardView>).payload,
    }
  );
};
