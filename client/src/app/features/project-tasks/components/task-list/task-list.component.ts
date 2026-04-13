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
import { LucideListChecks, LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/auth/store/auth.selectors';

@Component({
  selector: 'app-task-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ListComponent,
    TaskListItemComponent,
    TaskInlineComponent,
    LucideListChecks,
    FlatButtonComponent,
    LucidePlus,
  ],
  template: `
    <app-list
      [items]="tasks()"
      [itemSize]="43"
      viewportClass="h-[calc(100vh-262px)] min-h-16"
      [trackBy]="trackByTask">
      <ng-template #item let-task>
        <app-task-list-item
          class="mb-0.75 block overflow-hidden rounded-sm"
          [task]="task" />
      </ng-template>

      <ng-template #listFooter>
        @if (canCreate()) {
          <app-task-inline [siblings]="tasks()" />
        }
      </ng-template>

      <ng-template #listEmpty>
        @if (canCreate()) {
          <app-task-inline />
        }

        <div class="flex justify-center">
          <div
            class="my-16 flex h-full flex-col items-center justify-center gap-2">
            <svg size="38" lucideListChecks></svg>
            <h4 class="mx-16 text-center font-normal">
              There are currently no tasks.
            </h4>

            @if (canCreate()) {
              <p class="text-foreground/70 mb-4 text-center text-sm">
                Use the Create Task button to create your first task and get
                started.
              </p>
              <button app-flat-button>
                <svg size="20" lucidePlus></svg>
                <span>Create Task</span>
              </button>
            }
          </div>
        </div>
      </ng-template>
    </app-list>
  `,
})
export class TaskListComponent {
  private store = inject(Store);

  readonly tasks = this.store.selectSignal(selectTasks);
  readonly trackByTask: TrackByFunction<TaskViewModel> = (_, task) => task.id;

  readonly canCreate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.create)
  );
}
