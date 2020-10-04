import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'px',
  pure: true,
})
export class PxPipe implements PipeTransform {
  transform(value: string | number): string {
    return value + 'px';
  }
}
