import { Injectable, Pipe, PipeTransform } from '@angular/core';
import dayjs from 'dayjs';
import RelativeTime from 'dayjs/plugin/relativeTime';
import UTC from 'dayjs/plugin/utc';
import LocalizedFormat from 'dayjs/plugin/localizedFormat';

@Pipe({
    name: 'fromNow',
    pure: true,
    standalone: false
})
@Injectable()
export class FromNowPipe implements PipeTransform {
  transform(value: string | Date | undefined | null): string {
    if (!value) {
      return '';
    }

    dayjs.extend(RelativeTime);
    dayjs.extend(UTC);
    dayjs.extend(LocalizedFormat);

    return dayjs.utc(value).local().fromNow();
  }
}
