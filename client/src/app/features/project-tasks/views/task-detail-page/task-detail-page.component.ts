import { Component, effect, inject, input } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PageLoadingComponent } from '@app/static/components/page-loading/page-loading.component';
import { EntityType } from '@core/models/entity-type';
import { StatusCategory } from '@core/models/status';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import { LucideCheck } from '@lucide/angular';
import { TaskDates } from '@static/components/task-dates/task-dates.component';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TaskDetailCommentsComponent } from '@entry/dialogs/task-detail-dialog/task-detail-comments.component';
import { TaskDetailDescriptionComponent } from '@entry/dialogs/task-detail-dialog/task-detail-description.component';
import { TaskDetailHeaderComponent } from '@entry/dialogs/task-detail-dialog/task-detail-header.component';
import { TaskDetailPropertiesComponent } from '@entry/dialogs/task-detail-dialog/task-detail-properties.component';
import { TaskDetailRelationsComponent } from '@entry/dialogs/task-detail-dialog/task-detail-relations.component';
import { TaskDetailTagsComponent } from '@entry/dialogs/task-detail-dialog/task-detail-tags.component';
import { TaskDetailActionsComponent } from '@entry/dialogs/task-detail-dialog/task-detail-actions.component';
import { TaskDetailService } from '@entry/dialogs/task-detail-dialog/task-detail.service';
import { PageContainerComponent } from '@app/static/components/page-container/page-container.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PERMISSONS } from '@app/core/auth/permissions';
import { Router } from '@angular/router';
import { TaskDetailFilesComponent } from '@entry/dialogs/task-detail-dialog/task-detail-files.component';
import { TaskDetailFlagsComponent } from '@entry/dialogs/task-detail-dialog/task-detail-flags.component';

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
            @if (readActivity()) {
              <app-activity-menu
                [entityType]="entityType"
                [entityId]="task.id" />
            }
          </div>
        </div>

        <div class="flex flex-col gap-12 px-6 lg:flex-row">
          <div class="flex grow flex-col">
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

          <div
            class="bg-card/40 mt-4 flex flex-col gap-6 rounded px-6 pb-6 lg:min-w-86">
            <app-task-detail-properties />
            <app-task-detail-actions />
          </div>
        </div>

        <div class="mt-7 flex justify-end">
          <app-task-dates [task]="task" />
        </div>
      } @else if (loadError(); as error) {
        <app-error-state
          [title]="
            error.status === 404
              ? 'This task could not be found'
              : 'This task could not be loaded'
          "
          [description]="
            error.status === 404
              ? 'It may have been deleted, or you may not have access to it.'
              : 'Check your connection and try again.'
          "
          [retryable]="error.status !== 404"
          [retrying]="loading()"
          (retry)="reload()" />
      } @else {
        <app-page-loading diameter="64" />
      }
    </app-page-container>
  `,
  imports: [
    LucideCheck,
    ErrorStateComponent,
    ActivityMenuComponent,
    PageLoadingComponent,
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
    PageContainerComponent,
    TaskDetailFilesComponent,
    TaskDetailFlagsComponent,
  ],
  providers: [TaskDetailService],
})
export class TaskDetailPageComponent {
  router = inject(Router);

  entityType = EntityType.task;
  statusCategory = StatusCategory;
  private readonly taskDetail = inject(TaskDetailService);

  task = this.taskDetail.task;
  loading = this.taskDetail.loading;
  loadError = this.taskDetail.loadError;

  systemId = input.required<string>();

  readTags = hasPermission(PERMISSONS.tags.read);

  readFiles = hasPermission(PERMISSONS.files.read);

  readFlags = hasPermission(PERMISSONS.flags.read);

  readComments = hasPermission(PERMISSONS.comments.read);

  readActivity = hasPermission(PERMISSONS.activity.read);

  constructor() {
    effect(() => {
      const systemId = this.systemId();

      if (systemId) {
        this.taskDetail.show(systemId);
      }
    });
  }

  reload() {
    this.taskDetail.reload();
  }
}
