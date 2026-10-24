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
    case EntityType.sprint:
      return 'Sprint';
    case EntityType.status:
      return 'Status';
    case EntityType.task:
      return 'Task';
    case EntityType.user:
      return 'User';
    case EntityType.workspace:
      return 'Workspace';
    case EntityType.relationType:
      return $localize`:Name of the relation type entity in activity and audit logs:Relation type`;
    case EntityType.workspaceFile:
      return $localize`:Name of the workspace file entity in activity and audit logs:File`;
    case EntityType.page:
      return $localize`:Name of the page entity in activity and audit logs:Page`;
    case EntityType.automationRule:
      return $localize`:Name of the automation rule entity in activity and audit logs:Automation`;
    default:
      return '[UNKNOWN ENTITY TYPE]';
  }
};
