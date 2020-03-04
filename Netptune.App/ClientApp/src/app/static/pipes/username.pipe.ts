import { Pipe, PipeTransform } from '@angular/core';
import { toDisplay } from '@core/models/converters/username.converter';

@Pipe({
  name: 'username',
})
export class UsernamePipe implements PipeTransform {
  transform(value: any, ...args: any[]): any {
    return toDisplay(value);
  }
}
