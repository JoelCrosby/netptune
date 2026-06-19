import { Pipe, PipeTransform } from '@angular/core';
import { TaskStatus } from '@core/enums/project-task-status';

@Pipe({
  name: 'sprintBacklogStatusBadgeClass',
  pure: true,
  standalone: true,
})
export class SprintBacklogStatusBadgeClassPipe implements PipeTransform {
  transform(status: TaskStatus): string {
    switch (status) {
      case TaskStatus.new:
        return 'bg-blue-100 text-blue-700';
      case TaskStatus.inProgress:
        return 'bg-yellow-100 text-yellow-700';
      case TaskStatus.complete:
        return 'bg-green-100 text-green-700';
      case TaskStatus.onHold:
        return 'bg-purple-100 text-purple-700';
      default:
        return 'bg-neutral-100 text-neutral-600';
    }
  }
}
