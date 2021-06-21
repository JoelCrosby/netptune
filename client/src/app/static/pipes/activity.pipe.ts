import { Pipe, PipeTransform } from '@angular/core';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';
import { activityTypeToString } from '@core/transforms/activity-type';
import { entityTypeToString } from '@core/transforms/entity-type';
import { FromNowPipe } from './from-now.pipe';

@Pipe({
  name: 'activity',
  pure: true,
})
export class ActivityPipe implements PipeTransform {
  constructor(private fromNow: FromNowPipe) {}

  transform(value: ActivityViewModel): string {
    const activityType = activityTypeToString(value.type);
    const entityType = entityTypeToString(value.entityType);

    const action = `${activityType} ${entityType}`;

    const time = this.fromNow.transform(value.time);

    return `${action} ${time}`;
  }
}
