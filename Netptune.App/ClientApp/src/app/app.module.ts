// Modules
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { SharedModule } from './shared/shared.module';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { CoreModule } from './core';

// Components
import { AppComponent } from './app.component';
import { ConfirmDialogComponent } from './dialogs/confirm-dialog/confirm-dialog.component';
import { ProjectDialogComponent } from './dialogs/project-dialog/project-dialog.component';
import { TaskDialogComponent } from './dialogs/task-dialog/task-dialog.component';
import { InviteDialogComponent } from './dialogs/invite-dialog/invite-dialog.component';
import { BoardPostDialogComponent } from './dialogs/board-post-dialog/board-post-dialog.component';
import { TaskDetailDialogComponent } from './dialogs/task-detail-dialog/task-detail-dialog.component';

@NgModule({
  declarations: [
    AppComponent,
    ProjectDialogComponent,
    ConfirmDialogComponent,
    TaskDialogComponent,
    TaskDetailDialogComponent,
    InviteDialogComponent,
    BoardPostDialogComponent,
  ],
  entryComponents: [
    ProjectDialogComponent,
    ConfirmDialogComponent,
    TaskDialogComponent,
    TaskDetailDialogComponent,
    InviteDialogComponent,
    BoardPostDialogComponent,
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    DragDropModule,
    CoreModule,
    SharedModule,
    AppRoutingModule,
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
