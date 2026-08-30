import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, effect, inject, untracked } from '@angular/core';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { TaskDetailCockpitComponent } from './layouts/task-detail-cockpit.component';
import { TaskDetailDocumentComponent } from './layouts/task-detail-document.component';
import { TaskDetailSummaryRailComponent } from './layouts/task-detail-summary-rail.component';
import { TaskDetailLayoutService } from './task-detail-layout';
import { TaskDetailService } from './task-detail.service';
import { TaskDetailSkeletonComponent } from './task-detail-skeleton.component';

export interface TaskDetailDialogData {
  systemId: string;
}

@Component({
  selector: 'app-task-detail-dialog',
  template: `
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
    } @else {
      <app-task-detail-skeleton />
    }
  `,
  host: { class: 'block h-full min-h-0' },
  imports: [
    TaskDetailCockpitComponent,
    TaskDetailDocumentComponent,
    TaskDetailSummaryRailComponent,
    TaskDetailSkeletonComponent,
  ],
  providers: [TaskDetailService],
})
export class TaskDetailDialogComponent {
  data = inject<TaskDetailDialogData>(DIALOG_DATA, { optional: false });

  private dialogRef = inject<DialogRef<TaskDetailDialogComponent>>(DialogRef);
  private snackbar = inject(SnackbarService);
  private taskDetail = inject(TaskDetailService);

  readonly layout = inject(TaskDetailLayoutService);

  public static readonly width = '1140px';
  public static readonly height = '740px';
  public static readonly panelClass = 'app-task-detail-dialog';

  task = this.taskDetail.task;

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
