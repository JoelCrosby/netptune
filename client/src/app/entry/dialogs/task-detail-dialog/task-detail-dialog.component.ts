import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, effect, inject, untracked } from '@angular/core';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { EntityType } from '@core/models/entity-type';
import { StatusCategory } from '@core/models/status';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import { LucideCheck } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
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
import { TaskDetailFilesComponent } from './task-detail-files.component';
import { TaskDetailFlagsComponent } from './task-detail-flags.component';

export interface TaskDetailDialogData {
  systemId: string;
}

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
            @if (readActivity()) {
              <app-activity-menu
                [entityType]="entityType"
                [entityId]="task.id" />
            }
          </div>
        </div>

        <div class="flex flex-row gap-12 px-6">
          <div class="flex w-64 grow flex-col">
            @if (readTags()) {
              <app-task-detail-tags />
            }

            @if (readFlags()) {
              <app-task-detail-flags />
            }

            <app-task-detail-description />
            @if (readFiles()) {
              <app-task-detail-files [systemId]="task.systemId" />
            }
            <app-task-detail-relations />
            @if (readComments()) {
              <app-task-detail-comments />
            }
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
    TaskDetailFilesComponent,
    TaskDetailFlagsComponent,
  ],
  providers: [TaskDetailService],
})
export class TaskDetailDialogComponent {
  data = inject<TaskDetailDialogData>(DIALOG_DATA, { optional: false });
  store = inject(Store);
  private dialogRef = inject<DialogRef<TaskDetailDialogComponent>>(DialogRef);
  private snackbar = inject(SnackbarService);
  private taskDetail = inject(TaskDetailService);

  public static width = '972px';

  entityType = EntityType.task;
  statusCategory = StatusCategory;

  task = this.taskDetail.task;

  readTags = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tags.read)
  );

  readFiles = this.store.selectSignal(
    selectHasPermission(netptunePermissions.files.read)
  );

  readFlags = this.store.selectSignal(
    selectHasPermission(netptunePermissions.flags.read)
  );

  readComments = this.store.selectSignal(
    selectHasPermission(netptunePermissions.comments.read)
  );

  readActivity = this.store.selectSignal(
    selectHasPermission(netptunePermissions.activity.read)
  );

  constructor() {
    this.taskDetail.show(this.data.systemId);

    effect(() => {
      const wasRemoved = this.taskDetail.loadError()?.status === 404;

      if (!wasRemoved) return;

      untracked(() => {
        this.snackbar.error(
          $localize`:Shown when the open task no longer exists:This task no longer exists.`
        );
        this.dialogRef.close();
      });
    });
  }
}
