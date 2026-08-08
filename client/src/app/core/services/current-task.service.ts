import { Injectable, signal } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';

/** The task the user has open, for anything outside the detail view that needs to know. */
@Injectable({ providedIn: 'root' })
export class CurrentTaskService {
  private readonly open = signal<TaskViewModel | undefined>(undefined);

  readonly task = this.open.asReadonly();

  set(task: TaskViewModel | undefined) {
    this.open.set(task);
  }
}
