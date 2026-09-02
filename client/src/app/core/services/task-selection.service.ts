import { computed, Service, signal } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';

@Service()
export class TaskSelectionService {
  private readonly selected = signal<TaskViewModel[]>([]);

  readonly tasks = this.selected.asReadonly();
  readonly taskIds = computed(() => this.selected().map((task) => task.id));

  set(tasks: TaskViewModel[]) {
    this.selected.set(tasks);
  }

  clear() {
    this.selected.set([]);
  }
}
