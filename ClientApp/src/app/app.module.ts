// Modules
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RouterModule } from '@angular/router';
import { AppMaterialModule } from './modules/app-material/app-material.module';
import { DragulaModule } from 'ng2-dragula';
import { QuillModule } from 'ngx-quill';

// Components
import { AppComponent } from './components/app/app.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { DescriptorsComponent } from './components/descriptors/descriptors.component';
import { ConfirmDialogComponent } from './components/dialogs/confirm-dialog/confirm-dialog.component';
import { ProjectDialogComponent } from './components/dialogs/project-dialog/project-dialog.component';
import { ProjectTypeDialogComponent } from './components/dialogs/project-type-dialog/project-type-dialog.component';
import { TaskDialogComponent } from './components/dialogs/task-dialog/task-dialog.component';
import { WorkspaceDialogComponent } from './components/dialogs/workspace-dialog/workspace-dialog.component';
import { FlagsComponent } from './components/flags/flags.component';
import { HeroComponent } from './components/hero/hero.component';
import { LoginComponent } from './components/login/login.component';
import { NavMenuComponent } from './components/nav-menu/nav-menu.component';
import { ProjectTypesComponent } from './components/project-types/project-types.component';
import { ProjectsComponent } from './components/projects/projects.component';
import { RegisterComponent } from './components/register/register.component';
import { SideBarComponent } from './components/side-bar/side-bar.component';
import { ProjectTasksComponent } from './components/project-tasks/project-tasks.component';
import { UsersComponent } from './components/users/users.component';
import { WorkspacesComponent } from './components/workspaces/workspaces.component';
import { ProfileComponent } from './components/profile/profile.component';
import { TaskListComponent } from './components/project-tasks/task-list/task-list.component';
import { EditorComponent } from './components/editor/editor.component';
import { InviteDialogComponent } from './components/dialogs/invite-dialog/invite-dialog.component';
import { BoardPostsComponent } from './components/board-posts/board-posts.component';
import { BoardPostDialogComponent } from './components/dialogs/board-post-dialog/board-post-dialog.component';

// Services
import { AuthService } from './services/auth/auth.service';
import { AuthGuardService } from './services/auth/auth-guard.service';
import { ProjectTaskService } from './services/project-task/project-task.service';
import { ProjectTypeService } from './services/project-type/project-type.service';
import { ProjectsService } from './services/projects/projects.service';
import { TransitionService } from './services/transition/transition.service';
import { UserService } from './services/user/user.service';
import { WorkspaceService } from './services/workspace/workspace.service';
import { AlertService } from './services/alert/alert.service';
import { LayoutService } from './services/layout/layout.service';
import { UtilService } from './services/util/util.service';
import { PostsService } from './services/posts/posts.service';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    SideBarComponent,
    DashboardComponent,
    LoginComponent,
    RegisterComponent,
    ProjectsComponent,
    WorkspacesComponent,
    ProjectTasksComponent,
    UsersComponent,
    FlagsComponent,
    DescriptorsComponent,
    ProjectTypesComponent,
    ProjectDialogComponent,
    HeroComponent,
    WorkspaceDialogComponent,
    ConfirmDialogComponent,
    ProjectTypeDialogComponent,
    TaskDialogComponent,
    ProfileComponent,
    TaskListComponent,
    EditorComponent,
    InviteDialogComponent,
    BoardPostsComponent,
    BoardPostDialogComponent
  ],
  entryComponents: [
    ProjectDialogComponent,
    WorkspaceDialogComponent,
    ProjectTypeDialogComponent,
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
    DragulaModule.forRoot(),
    RouterModule.forRoot([
      { path: '', component: WorkspacesComponent, canActivate: [AuthGuardService], pathMatch: 'full' },
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent },
      { path: 'projects', component: ProjectsComponent, canActivate: [AuthGuardService] },
      { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuardService] },
      { path: 'workspaces', component: WorkspacesComponent, canActivate: [AuthGuardService] },
      { path: 'tasks', component: ProjectTasksComponent, canActivate: [AuthGuardService], runGuardsAndResolvers: 'always' },
      { path: 'flags', component: FlagsComponent, canActivate: [AuthGuardService] },
      { path: 'users', component: UsersComponent, canActivate: [AuthGuardService] },
      { path: 'descriptors', component: DescriptorsComponent, canActivate: [AuthGuardService] },
      { path: 'profile', component: ProfileComponent, canActivate: [AuthGuardService] },
      { path: '**', redirectTo: 'workspaces' }
    ], { onSameUrlNavigation: 'reload' })
  ],
  providers: [
    AuthService,
    AuthGuardService,
    ProjectsService,
    ProjectTypeService,
    WorkspaceService,
    AlertService,
    ProjectTaskService,
    TransitionService,
    UserService,
    UtilService,
    LayoutService,
    PostsService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
