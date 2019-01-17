// Modules
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { AppRoutingModule } from './routing/app-routing.module';
import { AppMaterialModule } from './modules/app-material/app-material.module';
import { DragDropModule } from '@angular/cdk/drag-drop';

// Components
import { AppComponent } from './components/app/app.component';
import { ConfirmDialogComponent } from './dialogs/confirm-dialog/confirm-dialog.component';
import { ProjectDialogComponent } from './dialogs/project-dialog/project-dialog.component';
import { TaskDialogComponent } from './dialogs/task-dialog/task-dialog.component';
import { SideBarComponent } from './components/side-bar/side-bar.component';
import { InviteDialogComponent } from './dialogs/invite-dialog/invite-dialog.component';
import { BoardPostDialogComponent } from './dialogs/board-post-dialog/board-post-dialog.component';

// Services
import { ProjectTaskService } from './services/project-task/project-task.service';
import { ProjectsService } from './services/projects/projects.service';
import { TransitionService } from './services/transition/transition.service';
import { UserService } from './services/user/user.service';
import { WorkspaceService } from './services/workspace/workspace.service';
import { AlertService } from './services/alert/alert.service';
import { UtilService } from './services/util/util.service';
import { PostsService } from './services/posts/posts.service';
import { AuthService } from './services/auth/auth.service';
import { AuthGuardService } from './services/auth/auth-guard.service';

@NgModule({
  declarations: [
    AppComponent,
    SideBarComponent,
    ProjectDialogComponent,
    ConfirmDialogComponent,
    TaskDialogComponent,
    InviteDialogComponent,
    BoardPostDialogComponent,
  ],
  entryComponents: [
    ProjectDialogComponent,
    ConfirmDialogComponent,
    TaskDialogComponent,
    InviteDialogComponent,
    BoardPostDialogComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    AppMaterialModule,
    DragDropModule,
    AppRoutingModule
  ],
  providers: [
    AuthService,
    AuthGuardService,
    ProjectsService,
    WorkspaceService,
    AlertService,
    ProjectTaskService,
    TransitionService,
    UserService,
    UtilService,
    PostsService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
