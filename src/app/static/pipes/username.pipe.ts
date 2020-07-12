import { Pipe, PipeTransform } from '@angular/core';
import { toDisplay } from '@core/models/converters/username.converter';
import { AppUser } from '@app/core/models/appuser';

@Pipe({
  name: 'username',
})
export class UsernamePipe implements PipeTransform {
  transform(value: AppUser): string {
    return toDisplay(value);
  }
}
