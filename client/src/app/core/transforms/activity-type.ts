import { ActivityType } from '@core/models/view-models/activity-view-model';

export const activityTypeToString = (value: ActivityType): string => {
  switch (value) {
    case ActivityType.assign:
      return 'assigned';
    case ActivityType.create:
      return 'created';
    case ActivityType.delete:
      return 'deleted';
    case ActivityType.modify:
      return 'modified';
    case ActivityType.move:
      return 'moved';
    case ActivityType.reorder:
      return 'reordered';
    case ActivityType.flag:
      return 'flagged';
    case ActivityType.unFlag:
      return 'un-flagged';
    case ActivityType.modifyName:
      return 'modified name';
    case ActivityType.modifyDescription:
      return 'modified description';
    case ActivityType.modifyStatus:
      return 'changed status';
    default:
      return '[UNKNOWN ACTIVITY TYPE]';
  }
};
