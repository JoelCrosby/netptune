import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'sprintBacklogStatusLabel',
  pure: true,
  standalone: true,
})
export class SprintBacklogStatusLabelPipe implements PipeTransform {
  transform(status: string | null | undefined): string {
    return status ?? 'Unknown';
  }
}
