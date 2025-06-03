import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'avatarFontSize',
    pure: true,
    standalone: false
})
export class AvatarFontSizePipe implements PipeTransform {
  transform(value: string | number | undefined | null): number {
    if (typeof value === 'number') return value / 2;

    if (value === undefined || value === null) {
      return 24;
    }

    return Number.parseInt(value, 2) / 2;
  }
}
