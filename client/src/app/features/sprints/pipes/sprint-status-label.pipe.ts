import { Pipe, PipeTransform } from '@angular/core';
import { SprintStatus, sprintStatusLabels } from '@core/enums/sprint-status';

@Pipe({
  name: 'sprintStatusLabel',
  pure: true,
  standalone: true,
})
export class SprintStatusLabelPipe implements PipeTransform {
  transform(status: SprintStatus): string {
    return sprintStatusLabels[status];
  }
}
