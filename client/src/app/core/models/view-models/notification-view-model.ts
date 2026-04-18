import { ActivityType } from './activity-view-model';
import { EntityType } from '../entity-type';

export interface NotificationViewModel {
  id: number;
  isRead: boolean;
  link: string | null;
  entityType: EntityType;
  activityType: ActivityType;
  createdAt: Date;
  actorUserId: string;
  actorUsername: string;
  actorPictureUrl: string | null;
  entityName: string | null;
  entityIdentifier: string | null;
}
