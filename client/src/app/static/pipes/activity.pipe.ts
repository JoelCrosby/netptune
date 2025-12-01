import { Pipe, PipeTransform, inject } from '@angular/core';
import {
  ActivityType,
  ActivityViewModel,
} from '@core/models/view-models/activity-view-model';
import { activityTypeToString } from '@core/transforms/activity-type';
import { FromNowPipe } from './from-now.pipe';

@Pipe({
  name: 'activity',
  pure: true,
})
export class ActivityPipe implements PipeTransform {
  private fromNow = inject(FromNowPipe);

  transform(value: ActivityViewModel): string {
    const activityType = activityTypeToString(value.type);
    const meta = getMeta(value);
    const action = `${activityType} ${meta}`;

    const time = this.fromNow.transform(value.time);

    return `${action} ${time}`;
  }
}

const getMeta = (value: ActivityViewModel) => {
  const meta = value.meta as { group: string };

  switch (value.type) {
    case ActivityType.move:
      return `to ${meta?.group ?? ''} group`;
    case ActivityType.assign:
      return `to ${value.assignee?.displayName ?? '(removed user)'}`;
    default:
      return '';
  }
};
