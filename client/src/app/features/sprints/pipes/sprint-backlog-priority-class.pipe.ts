import { Pipe, PipeTransform } from '@angular/core';
import { TaskPriority } from '@core/enums/task-priority';

@Pipe({
  name: 'sprintBacklogPriorityClass',
  pure: true,
  standalone: true,
})
export class SprintBacklogPriorityClassPipe implements PipeTransform {
  transform(priority: TaskPriority | null | undefined): string {
    switch (priority) {
      case TaskPriority.critical:
        return 'text-red-500';
      case TaskPriority.high:
        return 'text-orange-400';
      case TaskPriority.medium:
        return 'text-yellow-500';
      case TaskPriority.low:
        return 'text-blue-400';
      default:
        return 'text-zinc-400';
    }
  }
}
