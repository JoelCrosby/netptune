import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-task-scope-id',
  template: ` <div
    class="bg-primary/10 dark:bg-primary/30 rounded-sm px-2 py-1 text-xs font-semibold whitespace-nowrap text-black dark:text-white/80">
    {{ id() }}
  </div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
})
export class TaskScopeIdComponent {
  id = input.required<string>();
}
