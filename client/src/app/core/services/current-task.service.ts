import { Injectable, signal } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';

@Injectable({ providedIn: 'root' })
export class CurrentTaskService {
  private readonly open = signal<TaskViewModel | undefined>(undefined);

  readonly task = this.open.asReadonly();

  set(task: TaskViewModel | undefined) {
    this.open.set(task);
  }
}
