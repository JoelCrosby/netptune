// Modules
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { AppRoutingModule } from './modules/routing/app-routing.module';
import { AppMaterialModule } from './modules/app-material/app-material.module';
import { QuillModule } from 'ngx-quill';
import { DragDropModule } from '@angular/cdk/drag-drop';

// Components
import { AppComponent } from './components/app/app.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ConfirmDialogComponent } from './components/dialogs/confirm-dialog/confirm-dialog.component';
import { ProjectDialogComponent } from './components/dialogs/project-dialog/project-dialog.component';
import { TaskDialogComponent } from './components/dialogs/task-dialog/task-dialog.component';
import { FlagsComponent } from './components/flags/flags.component';
import { HeroComponent } from './components/hero/hero.component';
import { ProjectsComponent } from './components/projects/projects.component';
import { SideBarComponent } from './components/side-bar/side-bar.component';
import { ProjectTasksComponent } from './components/project-tasks/project-tasks.component';
import { UsersComponent } from './components/users/users.component';
import { ProfileComponent } from './components/profile/profile.component';
import { TaskListComponent } from './components/project-tasks/task-list/task-list.component';
import { EditorComponent } from './components/editor/editor.component';
import { InviteDialogComponent } from './components/dialogs/invite-dialog/invite-dialog.component';
import { BoardPostsComponent } from './components/board-posts/board-posts.component';
import { BoardPostDialogComponent } from './components/dialogs/board-post-dialog/board-post-dialog.component';
import { BoardsComponent } from './components/boards/boards.component';
import { TaskBoardComponent } from './components/boards/task-board/task-board.component';

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
    DashboardComponent,
    ProjectsComponent,
    ProjectTasksComponent,
    UsersComponent,
    FlagsComponent,
    ProjectDialogComponent,
    HeroComponent,
    ConfirmDialogComponent,
    TaskDialogComponent,
    ProfileComponent,
    TaskListComponent,
    EditorComponent,
    InviteDialogComponent,
    BoardPostsComponent,
    BoardPostDialogComponent,
    BoardsComponent,
    TaskBoardComponent
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
    QuillModule,
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
