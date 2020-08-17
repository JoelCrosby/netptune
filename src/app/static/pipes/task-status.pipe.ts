import { Pipe, PipeTransform } from '@angular/core';
import { TaskStatus } from '@core/enums/project-task-status';

@Pipe({
  name: 'taskStatus',
})
export class TaskStatusPipe implements PipeTransform {
  transform(value: TaskStatus): string {
    return TaskStatus[value];
  }
}
