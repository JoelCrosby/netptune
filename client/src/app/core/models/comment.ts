import { EntityType } from './entity-type';

export interface CommentViewModel {
  id: number;
  userDisplayName: string;
  userDisplayImage: string;
  userId: number;
  body: string;
  entityId: number;
  entityType: EntityType;
  reactions: Reaction[];
  createdAt: Date;
  updatedAt: Date;
}

export interface Reaction {
  id: number;
  value: string;
}
