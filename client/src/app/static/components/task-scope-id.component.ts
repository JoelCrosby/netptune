import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-task-scope-id',
  template: ` <div
    class="rounded-sm text-center px-2 text-sm mr-[0.6rem] bg-primary text-black">
    {{ id() }}
  </div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
})
export class TaskScopeIdComponent {
  id = input.required<string>();
}
