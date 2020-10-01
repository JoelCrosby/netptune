import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'avatarFontSize',
  pure: true,
})
export class AvatarFontSizePipe implements PipeTransform {
  transform(value: string | number): number {
    if (typeof value === 'number') return value / 2;

    return Number.parseInt(value, 2) / 2;
  }
}
