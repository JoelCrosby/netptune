import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

// Services
import { AuthGuardService } from '../services/auth/auth-guard.service';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'tasks',
    canActivate: [AuthGuardService],
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: '../modules/auth/auth.module#AuthModule'
  },
  {
    path: 'projects',
    loadChildren: '../modules/projects/projects.module#ProjectsModule',
    canActivate: [AuthGuardService]
  },
  {
    path: 'projects-tasks',
    loadChildren: '../modules/project-tasks/project-tasks.module#ProjectTasksModule',
    canActivate: [AuthGuardService]
  },
  {
    path: 'workspaces',
    loadChildren: '../modules/workspaces/workspaces.module#WorkspacesModule',
    canActivate: [AuthGuardService]
  },
  {
    path: 'tasks',
    loadChildren: '../modules/project-tasks/project-tasks.module#ProjectTasksModule',
    canActivate: [AuthGuardService],
    runGuardsAndResolvers: 'always'
  },
  {
    path: 'users',
    loadChildren: '../modules/users/users.module#UsersModule',
    canActivate: [AuthGuardService]
  },
  {
    path: 'profile',
    loadChildren: '../modules/profile/profile.module#ProfileModule',
    canActivate: [AuthGuardService]
  },
  {
    path: '**',
    redirectTo: 'auth'
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { onSameUrlNavigation: 'reload' })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
