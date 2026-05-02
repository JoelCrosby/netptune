import { DIALOG_DATA } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
} from '@angular/core';
import {
  clearTaskDetail,
  loadTaskDetails,
} from '@app/core/store/tasks/tasks.actions';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { EntityType } from '@core/models/entity-type';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { selectCurrentHubGroupId } from '@core/store/hub-context/hub-context.selectors';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import { LucideCheck } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { TaskDates } from '@static/components/task-dates/task-dates.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { TaskDetailCommentsComponent } from './task-detail-comments.component';
import { TaskDetailDescriptionComponent } from './task-detail-description.component';
import { TaskDetailHeaderComponent } from './task-detail-header.component';
import { TaskDetailPropertiesComponent } from './task-detail-properties.component';
import { TaskDetailTagsComponent } from './task-detail-tags.component';
import { TaskDetailActionsComponent } from './task-detail-actions.component';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-dialog',
  template: `
    @if (task(); as task) {
      <div>
        <div class="mb-1 flex flex-row items-center justify-end gap-4">
          @if (task.status === 1) {
            <svg lucideCheck class="h-4 w-4 text-green-500"></svg>
          }
          <app-task-scope-id [id]="task.systemId" />
          <app-activity-menu [entityType]="entityType" [entityId]="task.id" />
        </div>
        <div class="flex flex-col gap-6 px-12">
          <app-task-detail-header />
          <app-task-detail-properties />
          <app-task-detail-actions />
          <app-task-detail-tags />
          <app-task-detail-comments />
          <app-task-detail-description />
        </div>
      </div>
      <div app-dialog-actions align="end">
        <app-task-dates [task]="task" />
      </div>
    } @else {
      <div class="flex h-243.5 flex-col items-center justify-center">
        <app-spinner diameter="64" />
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LucideCheck,
    ActivityMenuComponent,
    DialogActionsDirective,
    SpinnerComponent,
    TaskDates,
    TaskScopeIdComponent,
    TaskDetailPropertiesComponent,
    TaskDetailHeaderComponent,
    TaskDetailDescriptionComponent,
    TaskDetailCommentsComponent,
    TaskDetailTagsComponent,
    TaskDetailActionsComponent,
  ],
  providers: [TaskDetailService],
})
export class TaskDetailDialogComponent implements OnDestroy {
  data = inject<TaskViewModel>(DIALOG_DATA, { optional: false });
  store = inject(Store);

  public static width = '972px';

  entityType = EntityType.task;

  task = this.store.selectSignal(selectDetailTask);
  hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);

  constructor() {
    const systemId: string = this.data.systemId;
    this.store.dispatch(loadTaskDetails({ systemId }));
  }

  ngOnDestroy() {
    this.store.dispatch(clearTaskDetail());
  }
}
