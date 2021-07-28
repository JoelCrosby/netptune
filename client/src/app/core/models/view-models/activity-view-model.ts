import { EntityType } from '../entity-type';

export interface ActivityViewModel {
  entityType: EntityType;
  userId: string;
  userUsername: string;
  userPictureUrl: string;
  type: ActivityType;
  entityId: number;
  time: Date;
  meta: { [key: string]: unknown };
}

export enum ActivityType {
  create = 0,
  modify = 1,
  delete = 2,
  assign = 3,
  move = 5,
  reorder = 6,
  flag = 7,
  unFlag = 8,
  modifyName = 9,
  modifyDescription = 10,
  modifyStatus = 11,
}
