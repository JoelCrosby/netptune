import { EntityType } from '@core/models/entity-type';

export const entityTypeToString = (value: EntityType): string => {
  switch (value) {
    case EntityType.board:
      return 'Board';
    case EntityType.boardGroup:
      return 'Board group';
    case EntityType.comment:
      return 'Comment';
    case EntityType.project:
      return 'Project';
    case EntityType.task:
      return 'Task';
    case EntityType.user:
      return 'User';
    case EntityType.workspace:
      return 'Workspace';
    default:
      return '[UNKNOWN ENTITY TYPE]';
  }
};
