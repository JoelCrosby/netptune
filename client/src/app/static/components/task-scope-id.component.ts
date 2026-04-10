import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-task-scope-id',
  template: ` <div
    class="bg-primary/10 dark:bg-primary py-/5 mr-[0.6rem] rounded-sm px-2 text-center text-sm text-black">
    {{ id() }}
  </div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
})
export class TaskScopeIdComponent {
  id = input.required<string>();
}
