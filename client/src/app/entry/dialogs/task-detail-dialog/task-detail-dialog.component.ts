import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, OnDestroy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  clearTaskDetail,
  deleteProjectTask,
  loadTaskDetails,
} from '@app/core/store/tasks/tasks.actions';
import { selectDetailTask } from '@app/core/store/tasks/tasks.selectors';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { EntityType } from '@core/models/entity-type';
import { StatusCategory } from '@core/models/status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { selectCurrentHubGroupId } from '@core/store/hub-context/hub-context.selectors';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import { LucideCheck } from '@lucide/angular';
import { Actions, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskDates } from '@static/components/task-dates/task-dates.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { TaskDetailActionsComponent } from './task-detail-actions.component';
import { TaskDetailCommentsComponent } from './task-detail-comments.component';
import { TaskDetailDescriptionComponent } from './task-detail-description.component';
import { TaskDetailHeaderComponent } from './task-detail-header.component';
import { TaskDetailPropertiesComponent } from './task-detail-properties.component';
import { TaskDetailRelationsComponent } from './task-detail-relations.component';
import { TaskDetailTagsComponent } from './task-detail-tags.component';
import { TaskDetailService } from './task-detail.service';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';

@Component({
  selector: 'app-task-detail-dialog',
  template: `
    @if (task(); as task) {
      <div>
        <div
          class="mb-1 flex flex-row items-center justify-between gap-4 pr-6 pl-2">
          <app-task-detail-header />

          <div class="flex items-center gap-4">
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

        <div class="flex flex-row gap-12 px-6">
          <div class="flex w-64 grow flex-col">
            @if (readTags()) {
              <app-task-detail-tags />
            }

            <app-task-detail-description />
            <app-task-detail-relations />
            <app-task-detail-comments />
          </div>

          <div class="bg-card/40 mt-4 flex flex-col gap-6 rounded px-6 pb-6">
            <app-task-detail-properties />
            <app-task-detail-actions />
          </div>
        </div>
      </div>
      <div app-dialog-actions align="start">
        <app-task-dates [task]="task" />
      </div>
    } @else {
      <div class="flex h-243.5 flex-col items-center justify-center">
        <app-spinner diameter="64" />
      </div>
    }
  `,
  imports: [
    LucideCheck,
    ActivityMenuComponent,
    DialogActionsDirective,
    SpinnerComponent,
    SprintBadgeComponent,
    TaskDates,
    TaskScopeIdComponent,
    TaskDetailPropertiesComponent,
    TaskDetailHeaderComponent,
    TaskDetailDescriptionComponent,
    TaskDetailRelationsComponent,
    TaskDetailCommentsComponent,
    TaskDetailTagsComponent,
    TaskDetailActionsComponent,
  ],
  providers: [TaskDetailService],
})
export class TaskDetailDialogComponent implements OnDestroy {
  data = inject<TaskViewModel>(DIALOG_DATA, { optional: false });
  store = inject(Store);
  private dialogRef = inject<DialogRef<TaskDetailDialogComponent>>(DialogRef);
  private actions$ = inject(Actions);

  public static width = '972px';

  entityType = EntityType.task;
  statusCategory = StatusCategory;

  task = this.store.selectSignal(selectDetailTask);
  hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);

  readTags = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tags.read)
  );

  constructor() {
    const systemId: string = this.data.systemId;
    this.store.dispatch(loadTaskDetails.init({ systemId }));

    this.actions$
      .pipe(ofType(deleteProjectTask.success), takeUntilDestroyed())
      .subscribe(({ taskId }) => {
        if (taskId === this.task()?.id) {
          this.dialogRef.close();
        }
      });
  }

  ngOnDestroy() {
    this.store.dispatch(clearTaskDetail());
  }
}
