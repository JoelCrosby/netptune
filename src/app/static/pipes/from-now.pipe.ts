import { Pipe, PipeTransform } from '@angular/core';
import * as Moment from 'moment';

@Pipe({
  name: 'fromNow',
})
export class FromNowPipe implements PipeTransform {
  transform(value: Date): string {
    if (!value) {
      return '';
    }

    return Moment.utc(value).local().fromNow();
  }
}
