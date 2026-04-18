import { ActivityType } from '@core/models/view-models/activity-view-model';

export const activityTypeToString = (value: ActivityType): string => {
  switch (value) {
    case ActivityType.assign:
      return 'Assigned';
    case ActivityType.create:
      return 'Created';
    case ActivityType.delete:
      return 'Deleted';
    case ActivityType.modify:
      return 'Modified';
    case ActivityType.move:
      return 'Moved';
    case ActivityType.reorder:
      return 'Reordered';
    case ActivityType.flag:
      return 'Flagged';
    case ActivityType.unFlag:
      return 'Un-flagged';
    case ActivityType.modifyName:
      return 'Modified name';
    case ActivityType.modifyDescription:
      return 'Modified description';
    case ActivityType.modifyStatus:
      return 'Changed status';
    case ActivityType.invite:
      return 'Invited';
    case ActivityType.remove:
      return 'Removed';
    case ActivityType.permissionChanged:
      return 'Changed permissions';
    case ActivityType.unassign:
      return 'Unassigned';
    case ActivityType.addTag:
      return 'Added tag';
    case ActivityType.removeTag:
      return 'Removed tag';
    default:
      return '[UNKNOWN ACTIVITY TYPE]';
  }
};
