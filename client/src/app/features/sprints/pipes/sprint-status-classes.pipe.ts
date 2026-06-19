import { Pipe, PipeTransform } from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';

@Pipe({
  name: 'sprintStatusClasses',
  pure: true,
  standalone: true,
})
export class SprintStatusClassesPipe implements PipeTransform {
  transform(status: SprintStatus): string {
    switch (status) {
      case SprintStatus.active:
        return 'bg-green-100 text-green-800';
      case SprintStatus.planning:
        return 'bg-blue-100 text-blue-800';
      case SprintStatus.completed:
        return 'bg-neutral-100 text-neutral-700';
      case SprintStatus.cancelled:
        return 'bg-red-100 text-red-700';
    }
  }
}
