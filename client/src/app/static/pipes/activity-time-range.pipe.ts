import { LOCALE_ID, Pipe, PipeTransform, inject } from '@angular/core';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';
import { timeRange } from '@core/util/dates';

@Pipe({
  name: 'activityTimeRange',
  pure: true,
  standalone: true,
})
export class ActivityTimeRangePipe implements PipeTransform {
  private locale = inject(LOCALE_ID);

  transform(value: ActivityViewModel): string {
    return timeRange(value.firstTime, value.time, this.locale);
  }
}
