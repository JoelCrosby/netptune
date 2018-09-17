
// Modules
import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RouterModule } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AppMaterialModule } from './modules/app-material/app-material.module';

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

// Services
import { AuthService } from './services/auth/auth.service';
import { ProjectTaskService } from './services/project-task/project-task.service';
import { ProjectTypeService } from './services/project-type/project-type.service';
import { ProjectsService } from './services/projects/projects.service';
import { TransitionService } from './services/transition/transition.service';
import { UserService } from './services/user/user.service';
import { WorkspaceService } from './services/workspace/workspace.service';
import { AlertService } from './services/alert/alert.service';
import { ProfileComponent } from './components/profile/profile.component';

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
    ProfileComponent
  ],
  entryComponents: [
    ProjectDialogComponent,
    WorkspaceDialogComponent,
    ProjectTypeDialogComponent,
    ConfirmDialogComponent,
    TaskDialogComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    AppMaterialModule,
    NgbModule.forRoot(),
    RouterModule.forRoot([
      { path: '', component: WorkspacesComponent, pathMatch: 'full' },
      { path: 'projects', component: ProjectsComponent },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'workspaces', component: WorkspacesComponent },
      { path: 'tasks', component: ProjectTasksComponent },
      { path: 'flags', component: FlagsComponent },
      { path: 'users', component: UsersComponent },
      { path: 'login', component: LoginComponent },
      { path: 'descriptors', component: DescriptorsComponent },
      { path: 'register', component: RegisterComponent },
      { path: 'profile', component: ProfileComponent },
      { path: '**', component: WorkspacesComponent },
    ])
  ],
  providers: [
    AuthService,
    ProjectsService,
    ProjectTypeService,
    WorkspaceService,
    AlertService,
    ProjectTaskService,
    TransitionService,
    UserService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
