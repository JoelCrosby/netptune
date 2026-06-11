import { Component, inject, OnDestroy } from '@angular/core';
import {
  clearTaskDetail,
  loadTaskDetails,
} from '@app/core/store/tasks/tasks.actions';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { EntityType } from '@core/models/entity-type';
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
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-task-detail-page',
  template: `
    @if (task(); as task) {
      <div class="mx-auto max-w-4xl px-4 py-8">
        <div
          class="mb-1 flex flex-row items-center justify-end gap-4 pr-6 pl-2">
          @if (task.sprintName) {
            <app-sprint-badge
              [name]="task.sprintName"
              [status]="task.sprintStatus" />
          }
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
        <div class="mt-7 flex justify-end">
          <app-task-dates [task]="task" />
        </div>
      </div>
    } @else {
      <div class="flex h-full flex-col items-center justify-center">
        <app-spinner diameter="64" />
      </div>
    }
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
  ],
  providers: [TaskDetailService],
})
export class TaskDetailPageComponent implements OnDestroy {
  store = inject(Store);
  route = inject(ActivatedRoute);

  entityType = EntityType.task;
  task = this.store.selectSignal(selectDetailTask);

  constructor() {
    const systemId: string = this.route.snapshot.params['systemId'];
    this.store.dispatch(loadTaskDetails({ systemId }));
  }

  ngOnDestroy() {
    this.store.dispatch(clearTaskDetail());
  }
}
