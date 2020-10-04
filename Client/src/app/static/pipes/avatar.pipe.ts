import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'avatar',
  pure: true,
})
export class AvatarPipe implements PipeTransform {
  transform(value: unknown): unknown {
    if (typeof value !== 'string') {
      return null;
    }

    if (value.length < 1) return null;

    const words = value.split(' ').map((val) => val.trim());

    if (words.length === 1) return words[0][0];

    return `${words[0][0]}${words[1][0]}`;
  }
}
