import { Pipe, PipeTransform } from '@angular/core';
import { prettyDate } from '@core/util/dates';

@Pipe({
  name: 'prettyDate',
  pure: true,
})
export class PrettyDatePipe implements PipeTransform {
  transform(value: Date | undefined | null): string {
    return prettyDate(value);
  }
}
