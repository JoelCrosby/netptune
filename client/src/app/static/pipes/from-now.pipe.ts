import { Injectable, Pipe, PipeTransform } from '@angular/core';
import { fromNow } from '@core/util/dates';

@Pipe({
  name: 'fromNow',
  pure: true,
})
@Injectable()
export class FromNowPipe implements PipeTransform {
  transform(value: string | Date | undefined | null): string {
    return fromNow(value);
  }
}
