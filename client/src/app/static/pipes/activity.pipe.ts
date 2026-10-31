import { Pipe, PipeTransform } from '@angular/core';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';
import {
  activityEditCount,
  activityTypeToString,
  mergedActivitySummary,
} from '@core/transforms/activity-type';
import { activityDetail } from '@core/transforms/activity-detail';
import { fromNow } from '@core/util/dates';

@Pipe({
  name: 'activity',
  pure: true,
  standalone: true,
})
export class ActivityPipe implements PipeTransform {
  transform(value: ActivityViewModel): string {
    const action = getAction(value);
    const edits = activityEditCount(value.revisionCount);

    const time = fromNow(value.time);

    return `${action}${edits} ${time}`;
  }
}

const getAction = (value: ActivityViewModel) => {
  const merged = mergedActivitySummary(value);

  if (merged) return merged;

  const activityType = activityTypeToString(value.type);
  const meta = activityDetail(value);

  return `${activityType} ${meta}`;
};
