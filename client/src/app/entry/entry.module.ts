import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';
import { ConfirmDialogComponent } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { InviteDialogComponent } from '@entry/dialogs/invite-dialog/invite-dialog.component';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { SharedModule } from '@shared/shared.module';
import { BoardGroupDialogComponent } from './dialogs/board-group-dialog/board-group-dialog.component';
import { StaticModule } from '@static/static.module';
import { ActivityMenuComponent } from './components/activity-menu/activity-menu.component';

@NgModule({
    imports: [CommonModule, SharedModule, StaticModule, ProjectDialogComponent,
        ConfirmDialogComponent,
        CreateTaskDialogComponent,
        TaskDetailDialogComponent,
        InviteDialogComponent,
        WorkspaceDialogComponent,
        BoardGroupDialogComponent,
        ActivityMenuComponent],
})
export class EntryModule {}
