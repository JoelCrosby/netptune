import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';
import { ConfirmDialogComponent } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { TaskDialogComponent } from '@entry/dialogs/task-dialog/task-dialog.component';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { InviteDialogComponent } from '@entry/dialogs/invite-dialog/invite-dialog.component';
import { BoardPostDialogComponent } from '@entry/dialogs/board-post-dialog/board-post-dialog.component';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { SharedModule } from '@app/shared/shared.module';

@NgModule({
  declarations: [
    ProjectDialogComponent,
    ConfirmDialogComponent,
    TaskDialogComponent,
    TaskDetailDialogComponent,
    InviteDialogComponent,
    BoardPostDialogComponent,
    WorkspaceDialogComponent,
  ],
  imports: [CommonModule, SharedModule],
})
export class EntryModule {}
