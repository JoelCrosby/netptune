import { Pipe, PipeTransform } from '@angular/core';
import * as dayjs from 'dayjs';
import * as RelativeTime from 'dayjs/plugin/relativeTime';
import * as UTC from 'dayjs/plugin/utc';

@Pipe({
  name: 'fromNow',
  pure: true,
})
export class FromNowPipe implements PipeTransform {
  transform(value: string | Date): string {
    if (!value) {
      return '';
    }

    dayjs.extend(RelativeTime);
    dayjs.extend(UTC);

    return dayjs(value).utc().local().fromNow();
  }
}
