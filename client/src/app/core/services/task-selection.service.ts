import { Injectable, signal } from '@angular/core';

/** Which rows the task list has selected, shared with the actions bar above it. */
@Injectable({ providedIn: 'root' })
export class TaskSelectionService {
  private readonly selected = signal<number[]>([]);

  readonly taskIds = this.selected.asReadonly();

  set(taskIds: number[]) {
    this.selected.set(taskIds);
  }

  clear() {
    this.selected.set([]);
  }
}
