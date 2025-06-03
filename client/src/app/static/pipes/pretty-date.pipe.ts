import { Pipe, PipeTransform } from '@angular/core';
import * as dayjs from 'dayjs';
import * as RelativeTime from 'dayjs/plugin/relativeTime';
import * as UTC from 'dayjs/plugin/utc';
import * as LocalizedFormat from 'dayjs/plugin/localizedFormat';

@Pipe({
    name: 'prettyDate',
    pure: true,
    standalone: false
})
export class PrettyDatePipe implements PipeTransform {
  transform(value: Date | undefined | null): string {
    if (!value) {
      return '';
    }

    dayjs.extend(RelativeTime);
    dayjs.extend(UTC);
    dayjs.extend(LocalizedFormat);

    return dayjs.utc(value).local().format('llll');
  }
}
