import { activityTypeToString } from '@core/transforms/activity-type';
import { Pipe, PipeTransform } from '@angular/core';
import { ActivityType } from '@core/models/view-models/activity-view-model';

@Pipe({
    name: 'activityType',
    pure: true,
    standalone: false
})
export class ActivityTypePipe implements PipeTransform {
  transform(value: ActivityType): string {
    return activityTypeToString(value);
  }
}
