import { EntityType } from './entity-type';

export interface CommentMention {
  userId: string;
  displayName: string;
}

export interface CommentViewModel {
  id: number;
  userDisplayName: string;
  userDisplayImage: string;
  userIsServiceAccount?: boolean;
  userId: string;
  body: string;
  entityId: number;
  entityType: EntityType;
  reactions: Reaction[];
  mentions: CommentMention[];
  createdAt: Date;
  updatedAt: Date;
}

export interface Reaction {
  id: number;
  value: string;
}
