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
    default:
      return '[UNKNOWN ACTIVITY TYPE]';
  }
};
