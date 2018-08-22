
// Modules
import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AppMaterialModule } from './app-material/app-material.module';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

// Components
import { AppComponent } from './app.component';
import { NavMenuComponent } from './components/nav-menu/nav-menu.component';
import { SideBarComponent } from './components/side-bar/side-bar.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { ProjectsComponent } from './components/projects/projects.component';
import { WorkspacesComponent } from './components/workspaces/workspaces.component';
import { TasksComponent } from './components/tasks/tasks.component';
import { FlagsComponent } from './components/flags/flags.component';
import { UsersComponent } from './components/users/users.component';
import { DescriptorsComponent } from './components/descriptors/descriptors.component';
import { ProjectTypesComponent } from './components/project-types/project-types.component';

// Services
import { AuthService } from './services/auth/auth.service';
import { ProjectsService } from './services/projects/projects.service';
import { ProjectTypeService } from './services/project-type/project-type.service';
import { AlertService } from './services/alert/alert.service';
import { WorkspaceService } from './services/workspace/workspace.service';
import { TransitionService } from './services/transition/transition.service';
import { ProjectDialogComponent } from './components/projects/dialogs/project-dialog/project-dialog.component';
import { HeroComponent } from './componments/hero/hero.component';

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
    TasksComponent,
    UsersComponent,
    FlagsComponent,
    DescriptorsComponent,
    ProjectTypesComponent,
    ProjectDialogComponent,
    HeroComponent
  ],
  entryComponents: [
    ProjectDialogComponent
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
      { path: 'tasks', component: TasksComponent },
      { path: 'flags', component: FlagsComponent },
      { path: 'users', component: UsersComponent },
      { path: 'login', component: LoginComponent },
      { path: 'descriptors', component: DescriptorsComponent },
      { path: 'register', component: RegisterComponent },
      { path: '**', component: WorkspacesComponent },
    ])
  ],
  providers: [
    AuthService,
    ProjectsService,
    ProjectTypeService,
    WorkspaceService,
    AlertService,
    TransitionService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
