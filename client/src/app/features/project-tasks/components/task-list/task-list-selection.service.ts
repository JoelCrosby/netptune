import { Injectable, signal } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';

@Injectable()
export class TaskListSelectionService {
  readonly selectedTasks = signal<readonly TaskViewModel[]>([]);

  setSelected(tasks: readonly TaskViewModel[]) {
    this.selectedTasks.set(tasks);
  }

  clear() {
    this.selectedTasks.set([]);
  }
}
