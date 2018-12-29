import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

// Components
import { ProjectsComponent } from '../../components/projects/projects.component';
import { BoardsComponent } from '../../components/boards/boards.component';
import { DashboardComponent } from '../../components/dashboard/dashboard.component';
import { UsersComponent } from '../../components/users/users.component';
import { ProfileComponent } from '../../components/profile/profile.component';

// Services
import { AuthGuardService } from '../../services/auth/auth-guard.service';

const routes: Routes = [
  { path: '', redirectTo: 'tasks', canActivate: [AuthGuardService], pathMatch: 'full' },
  { path: 'auth', loadChildren: '../auth/auth.module#AuthModule' },
  { path: 'projects', component: ProjectsComponent, canActivate: [AuthGuardService] },
  { path: 'boards', component: BoardsComponent, canActivate: [AuthGuardService] },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuardService] },
  { path: 'workspaces', loadChildren: '../workspaces/workspaces.module#WorkspacesModule', canActivate: [AuthGuardService] },
  {
    path: 'tasks',
    loadChildren: '../project-tasks/project-tasks.module#ProjectTasksModule',
    canActivate: [AuthGuardService],
    runGuardsAndResolvers: 'always'
  },
  { path: 'users', component: UsersComponent, canActivate: [AuthGuardService] },
  { path: 'profile', component: ProfileComponent, canActivate: [AuthGuardService] },
  { path: '**', redirectTo: 'workspaces' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { onSameUrlNavigation: 'reload' })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
