import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { EntityType } from '../models/entity-type';
import { ActivityViewModel } from '../models/view-models/activity-view-model';
import { cursorResource } from './cursor.resource';
import { requestFrom } from './permission.resource';

export interface ActivityFeedRequest {
  entityType: EntityType;
  entityId: number;
}

export const activityResource = (
  request: Signal<ActivityFeedRequest | null>
) => {
  return cursorResource<ActivityViewModel>({
    request: requestFrom(request, (feed) => ({
      url: `api/activity/${feed.entityType}/${feed.entityId}`,
    })),
    permission: PERMISSIONS.activity.read,
    trackBy: (activity) => activity.id,
    parse: (response) => response.payload ?? [],
  });
};
