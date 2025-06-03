import { Pipe, PipeTransform } from '@angular/core';
import { TaskStatus } from '@core/enums/project-task-status';

@Pipe({
    name: 'taskStatus',
    pure: true,
    standalone: false
})
export class TaskStatusPipe implements PipeTransform {
  transform(value: TaskStatus): string {
    switch (value) {
      case TaskStatus.new:
        return 'New';
      case TaskStatus.complete:
        return 'Complete';
      case TaskStatus.inProgress:
        return 'InProgress';
      case TaskStatus.onHold:
        return 'OnHold';
      case TaskStatus.unAssigned:
        return 'Un-assigned';
      case TaskStatus.awaitingClassification:
        return 'Awaiting Classification';
      case TaskStatus.inActive:
        return 'Inactive';
      default:
        return 'unknown';
    }
  }
}
