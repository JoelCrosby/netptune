import { Component, input } from '@angular/core';

@Component({
  selector: 'app-task-scope-id',
  template: ` <div
    class="bg-primary/10 dark:bg-primary/30 w-fit rounded-sm px-2 py-1 font-mono text-xs font-semibold whitespace-nowrap text-black select-none dark:text-white/80">
    {{ id() }}
  </div>`,
  imports: [],
})
export class TaskScopeIdComponent {
  id = input.required<string>();
}
