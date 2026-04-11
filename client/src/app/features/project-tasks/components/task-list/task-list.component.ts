import {
  ChangeDetectionStrategy,
  Component,
  TrackByFunction,
  inject,
} from '@angular/core';
import { selectTasks } from '@core/store/tasks/tasks.selectors';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import { ListComponent } from '@static/components/list/list.component';
import { TaskListItemComponent } from './task-list-item.component';
import { TaskInlineComponent } from '../task-inline/task-inline.component';

@Component({
  selector: 'app-task-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ListComponent, TaskListItemComponent, TaskInlineComponent],
  template: `
    <app-list
      header="Tasks"
      [items]="tasks()"
      [itemSize]="43"
      viewportClass="h-[calc(100vh-262px)] min-h-16"
      [trackBy]="trackByTask"
    >
      <ng-template #item let-task>
        <app-task-list-item
          class="mb-[3px] block overflow-hidden rounded-sm"
          [task]="task"
        />
      </ng-template>

      <ng-template #listFooter>
        <app-task-inline [siblings]="tasks()" />
      </ng-template>

      <ng-template #listEmpty>
        <app-task-inline />
        <div class="flex justify-center">
          <div class="flex h-full flex-col items-center justify-center">
            <i class="far fa-compass my-8 text-[6rem]"></i>
            <h4 class="mx-16 mb-12 text-center text-sm font-normal">
              There are currently no tasks. Use the Create Task button above to create a task
            </h4>
          </div>
        </div>
      </ng-template>
    </app-list>
  `,
})
export class TaskListComponent {
  private store = inject(Store);

  tasks = this.store.selectSignal(selectTasks);

  readonly trackByTask: TrackByFunction<TaskViewModel> = (_, task) => task.id;
}
