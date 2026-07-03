import { Component, effect, inject, input, OnDestroy } from '@angular/core';
import {
  clearTaskDetail,
  loadTaskDetails,
} from '@app/core/store/tasks/tasks.actions';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { EntityType } from '@core/models/entity-type';
import { StatusCategory } from '@core/models/status';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import { LucideCheck } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { TaskDates } from '@static/components/task-dates/task-dates.component';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TaskDetailCommentsComponent } from '@entry/dialogs/task-detail-dialog/task-detail-comments.component';
import { TaskDetailDescriptionComponent } from '@entry/dialogs/task-detail-dialog/task-detail-description.component';
import { TaskDetailHeaderComponent } from '@entry/dialogs/task-detail-dialog/task-detail-header.component';
import { TaskDetailPropertiesComponent } from '@entry/dialogs/task-detail-dialog/task-detail-properties.component';
import { TaskDetailTagsComponent } from '@entry/dialogs/task-detail-dialog/task-detail-tags.component';
import { TaskDetailActionsComponent } from '@entry/dialogs/task-detail-dialog/task-detail-actions.component';
import { TaskDetailService } from '@entry/dialogs/task-detail-dialog/task-detail.service';
import { PageContainerComponent } from '@app/static/components/page-container/page-container.component';

@Component({
  selector: 'app-task-detail-page',
  template: `
    <app-page-container>
      @if (task(); as task) {
        <div class="flex items-center justify-between">
          <app-task-detail-header />

          <div
            class="mb-1 flex flex-row items-center justify-end gap-4 pr-6 pl-2">
            @if (task.sprintName) {
              <app-sprint-badge
                [name]="task.sprintName"
                [status]="task.sprintStatus" />
            }
            @if (task.statusCategory === statusCategory.done) {
              <svg lucideCheck class="h-4 w-4 text-green-500"></svg>
            }
            <app-task-scope-id [id]="task.systemId" />
            <app-activity-menu [entityType]="entityType" [entityId]="task.id" />
          </div>
        </div>

        <div class="flex flex-col gap-12 px-6 lg:flex-row">
          <div class="flex grow flex-col">
            <app-task-detail-tags />
            <app-task-detail-description />
            <app-task-detail-comments />
          </div>

          <div
            class="bg-card/40 mt-4 flex flex-col gap-6 rounded px-6 pb-6 lg:min-w-86">
            <app-task-detail-properties />
            <app-task-detail-actions />
          </div>
        </div>

        <div class="mt-7 flex justify-end">
          <app-task-dates [task]="task" />
        </div>
      } @else {
        <div class="flex h-full flex-col items-center justify-center">
          <app-spinner diameter="64" />
        </div>
      }
    </app-page-container>
  `,
  imports: [
    LucideCheck,
    ActivityMenuComponent,
    SpinnerComponent,
    SprintBadgeComponent,
    TaskDates,
    TaskScopeIdComponent,
    TaskDetailPropertiesComponent,
    TaskDetailHeaderComponent,
    TaskDetailDescriptionComponent,
    TaskDetailCommentsComponent,
    TaskDetailTagsComponent,
    TaskDetailActionsComponent,
    PageContainerComponent,
  ],
  providers: [TaskDetailService],
})
export class TaskDetailPageComponent implements OnDestroy {
  store = inject(Store);

  entityType = EntityType.task;
  statusCategory = StatusCategory;
  task = this.store.selectSignal(selectDetailTask);

  systemId = input.required<string>();

  constructor() {
    effect(() => {
      const systemId = this.systemId();

      if (systemId) {
        this.store.dispatch(loadTaskDetails.init({ systemId }));
      }
    });
  }

  ngOnDestroy() {
    this.store.dispatch(clearTaskDetail());
  }
}
