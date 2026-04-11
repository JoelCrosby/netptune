import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { selectTasks } from '@core/store/tasks/tasks.selectors';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { Store } from '@ngrx/store';
import {
  CdkVirtualScrollViewport,
  CdkFixedSizeVirtualScroll,
  CdkVirtualForOf,
} from '@angular/cdk/scrolling';
import { TaskListItemComponent } from './task-list-item.component';
import { TaskInlineComponent } from '../task-inline/task-inline.component';

@Component({
  selector: 'app-task-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CdkVirtualScrollViewport,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    TaskListItemComponent,
    TaskInlineComponent,
  ],
  template: `
    <div class="w-full">
      <h4 class="text-foreground/60 mb-2 text-sm font-normal tracking-[.25px]">
        Tasks
      </h4>
      <div
        class="bg-board-group mb-5 flex h-full min-h-[196px] flex-1 flex-col overflow-hidden rounded-sm p-2.5">
        @if (tasks()?.length) {
          <cdk-virtual-scroll-viewport
            class="h-[calc(100vh-262px)] min-h-16"
            itemSize="43"
            minBufferPx="1024"
            maxBufferPx="2048">
            <app-task-list-item
              class="mb-[3px] block overflow-hidden rounded-sm"
              *cdkVirtualFor="
                let taskItem of tasks();
                trackBy: trackByTask;
                templateCacheSize: 0
              "
              [task]="taskItem">
            </app-task-list-item>
            <app-task-inline [siblings]="tasks()" />
          </cdk-virtual-scroll-viewport>
        } @else {
          <app-task-inline />
          <div class="flex justify-center">
            <div class="flex h-full flex-col items-center justify-center">
              <i class="far fa-compass my-8 text-[6rem]"></i>
              <h4 class="mx-16 mb-12 text-center text-sm font-normal">
                There are currently no tasks. Use the Create Task button above
                to create a task
              </h4>
            </div>
          </div>
        }
      </div>
    </div>
  `,
})
export class TaskListComponent {
  private store = inject(Store);

  tasks = this.store.selectSignal(selectTasks);

  trackByTask(_: number, task: TaskViewModel) {
    return task.id;
  }
}
