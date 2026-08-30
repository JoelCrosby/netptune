import { Component, effect, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { TaskDetailCockpitComponent } from '@entry/dialogs/task-detail-dialog/layouts/task-detail-cockpit.component';
import { TaskDetailDocumentComponent } from '@entry/dialogs/task-detail-dialog/layouts/task-detail-document.component';
import { TaskDetailSummaryRailComponent } from '@entry/dialogs/task-detail-dialog/layouts/task-detail-summary-rail.component';
import { TaskDetailLayoutService } from '@entry/dialogs/task-detail-dialog/task-detail-layout';
import { TaskDetailService } from '@entry/dialogs/task-detail-dialog/task-detail.service';
import { TaskDetailSkeletonComponent } from '@entry/dialogs/task-detail-dialog/task-detail-skeleton.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';

@Component({
  selector: 'app-task-detail-page',
  template: `
    <app-page-container layout="list" [horizontalPadding]="false">
      @if (task()) {
        @switch (layout.layout()) {
          @case ('cockpit') {
            <app-task-detail-cockpit />
          }
          @case ('document') {
            <app-task-detail-document />
          }
          @default {
            <app-task-detail-summary-rail />
          }
        }
      } @else if (loadError(); as error) {
        <app-error-state
          class="p-8"
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
        <app-task-detail-skeleton />
      }
    </app-page-container>
  `,
  imports: [
    ErrorStateComponent,
    PageContainerComponent,
    TaskDetailCockpitComponent,
    TaskDetailDocumentComponent,
    TaskDetailSummaryRailComponent,
    TaskDetailSkeletonComponent,
  ],
  providers: [TaskDetailService],
})
export class TaskDetailPageComponent {
  readonly systemId = input.required<string>();

  readonly router = inject(Router);
  readonly layout = inject(TaskDetailLayoutService);

  private readonly taskDetail = inject(TaskDetailService);

  task = this.taskDetail.task;
  loading = this.taskDetail.loading;
  loadError = this.taskDetail.loadError;

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
