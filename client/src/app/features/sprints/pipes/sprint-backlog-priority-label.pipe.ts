import { Pipe, PipeTransform } from '@angular/core';
import { TaskPriority, taskPriorityLabels } from '@core/enums/task-priority';

@Pipe({
  name: 'sprintBacklogPriorityLabel',
  pure: true,
  standalone: true,
})
export class SprintBacklogPriorityLabelPipe implements PipeTransform {
  transform(priority: TaskPriority | null | undefined): string {
    return priority === null || priority === undefined
      ? $localize`:Shown in place of an empty value:None`
      : taskPriorityLabels[priority];
  }
}
