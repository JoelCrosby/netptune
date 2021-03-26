import { EntityType } from '../entity-type';

export interface ActivityViewModel {
  entityType: EntityType;
  userId: string;
  userUsername: string;
  userPictureUrl: string;
  type: ActivityType;
  entityId: number;
  time: Date;
}

export enum ActivityType {
  create = 0,
  modify = 1,
  delete = 2,
  assign = 3,
  move = 5,
}
