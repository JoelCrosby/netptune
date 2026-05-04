import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-task-list-filter-separator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="bg-foreground/10 h-6 w-px"></div>`,
})
export class TaskListFilterSeparatorComponent {}
