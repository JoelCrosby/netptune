import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'px',
  pure: true,
})
export class PxPipe implements PipeTransform {
  transform(value: string | number | undefined | null): string {
    if (value === undefined || value === null) {
      return '';
    }

    return value + 'px';
  }
}
