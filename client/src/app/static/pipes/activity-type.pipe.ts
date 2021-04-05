import { Pipe, PipeTransform } from '@angular/core';
import { ActivityType } from '@core/models/view-models/activity-view-model';

@Pipe({
  name: 'activityType',
  pure: true,
})
export class ActivityTypePipe implements PipeTransform {
  transform(value: ActivityType): string {
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
  }
}
