import { Signal } from '@angular/core';
import { netptunePermissions } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import { EntityType } from '../models/entity-type';
import { ActivityViewModel } from '../models/view-models/activity-view-model';
import { cursorResource } from './cursor-resource';

export interface ActivityFeedRequest {
  entityType: EntityType;
  entityId: number;
}

export const activityResource = (
  request: Signal<ActivityFeedRequest | null>
) => {
  return cursorResource<ActivityViewModel>(
    () => {
      const feed = request();

      if (!feed) return undefined;

      return { url: `api/activity/${feed.entityType}/${feed.entityId}` };
    },
    netptunePermissions.activity.read,
    {
      trackBy: (activity) => activity.id,
      parse: (response) => {
        return (response as ClientResponse<ActivityViewModel[]>).payload ?? [];
      },
    }
  );
};
