import { EntityType } from '@core/models/entity-type';

export const entityTypeToString = (value: EntityType): string => {
  switch (value) {
    case EntityType.board:
      return 'board';
    case EntityType.boardGroup:
      return 'board group';
    case EntityType.comment:
      return 'comment';
    case EntityType.project:
      return 'project';
    case EntityType.task:
      return 'task';
    case EntityType.user:
      return 'user';
    case EntityType.workspace:
      return 'workspace';
    default:
      return '[UNKNOWN ENTITY TYPE]';
  }
};
