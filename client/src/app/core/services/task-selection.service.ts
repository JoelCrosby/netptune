import { Injectable, signal } from '@angular/core';

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
