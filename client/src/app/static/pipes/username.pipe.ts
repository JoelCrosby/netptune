import { Pipe, PipeTransform } from '@angular/core';
import { toDisplay } from '@core/models/converters/username.converter';
import { AppUser } from '@core/models/appuser';

@Pipe({
    name: 'username',
    pure: true,
    standalone: false
})
export class UsernamePipe implements PipeTransform {
  transform(value: AppUser): string {
    return toDisplay(value);
  }
}
