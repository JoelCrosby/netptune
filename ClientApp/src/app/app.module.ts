import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './components/nav-menu/nav-menu.component';
import { SideBarComponent } from './components/side-bar/side-bar.component';
import { HomeComponent } from './components/home/home.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { ProjectsComponent } from './components/projects/projects.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { TasksComponent } from './components/tasks/tasks.component';
import { FlagsComponent } from './components/flags/flags.component';
import { UsersComponent } from './components/users/users.component';

import { AuthService } from './services/auth/auth.service';
import { ProjectsService } from './services/projects/projects.service';
import { ProjectTypeService } from './services/project-type/project-type.service';
import { AlertService } from './services/alert/alert.service';
import { WorkspaceService } from './services/workspace/workspace.service';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    SideBarComponent,
    HomeComponent,
    LoginComponent,
    RegisterComponent,
    ProjectsComponent,
    DashboardComponent,
    TasksComponent,
    UsersComponent,
    FlagsComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    NgbModule.forRoot(),
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'projects', component: ProjectsComponent },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'tasks', component: TasksComponent },
      { path: 'flags', component: FlagsComponent },
      { path: 'users', component: UsersComponent },
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent },
      { path: '**', component: HomeComponent },
    ])
  ],
  providers: [
    AuthService,
    ProjectsService,
    ProjectTypeService,
    WorkspaceService,
    AlertService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
