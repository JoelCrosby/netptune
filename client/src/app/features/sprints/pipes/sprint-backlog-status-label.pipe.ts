import { Pipe, PipeTransform } from '@angular/core';
import { TaskStatus, taskStatusLabels } from '@core/enums/project-task-status';

@Pipe({
  name: 'sprintBacklogStatusLabel',
  pure: true,
  standalone: true,
})
export class SprintBacklogStatusLabelPipe implements PipeTransform {
  transform(status: TaskStatus): string {
    return taskStatusLabels[status] ?? 'unknown';
  }
}
