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
    case ActivityType.roleChanged:
      return 'Changed role';
    case ActivityType.workspaceSettingsChanged:
      return 'Changed workspace settings';
    case ActivityType.exportRequested:
      return 'Requested export';
    case ActivityType.loginSuccess:
      return 'Logged in';
    case ActivityType.loginFailed:
      return 'Failed login';
    case ActivityType.mention:
      return 'Mentioned';
    case ActivityType.modifyPriority:
      return 'Changed priority';
    case ActivityType.modifyEstimate:
      return 'Changed estimate';
    default:
      return '[UNKNOWN ACTIVITY TYPE]';
  }
};
