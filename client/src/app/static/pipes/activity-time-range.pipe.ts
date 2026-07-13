import { formatDate } from '@angular/common';
import { LOCALE_ID, Pipe, PipeTransform, inject } from '@angular/core';
import { ActivityViewModel } from '@core/models/view-models/activity-view-model';

@Pipe({
  name: 'activityTimeRange',
  pure: true,
  standalone: true,
})
export class ActivityTimeRangePipe implements PipeTransform {
  private locale = inject(LOCALE_ID);

  transform(value: ActivityViewModel): string {
    const last = formatDate(value.time, 'medium', this.locale);

    if (value.revisionCount <= 1) return last;

    const first = formatDate(value.firstTime, 'medium', this.locale);

    return `${first} – ${last}`;
  }
}
