import { Pipe, PipeTransform } from '@angular/core';
import { formatBytes } from '@core/util/bytes';

@Pipe({
  name: 'fileSize',
})
export class FileSizePipe implements PipeTransform {
  transform(bytes: number): string {
    return formatBytes(bytes);
  }
}
