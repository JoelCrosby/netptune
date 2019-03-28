import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { AuthGuardService } from './core/auth/auth-guard.service';

const routes: Routes = [
  {
    path: '',
    redirectTo: '/projects',
    canActivate: [AuthGuardService],
    pathMatch: 'full',
  },
  {
    path: 'projects',
    loadChildren: './features/projects/projects.module#ProjectsModule',
    canActivate: [AuthGuardService],
  },
  {
    path: 'projects-tasks',
    loadChildren: './features/project-tasks/project-tasks.module#ProjectTasksModule',
    canActivate: [AuthGuardService],
  },
  {
    path: 'workspaces',
    loadChildren: './features/workspaces/workspaces.module#WorkspacesModule',
    canActivate: [AuthGuardService],
  },
  {
    path: 'tasks',
    loadChildren: './features/project-tasks/project-tasks.module#ProjectTasksModule',
    canActivate: [AuthGuardService],
    runGuardsAndResolvers: 'always',
  },
  {
    path: 'users',
    loadChildren: './features/users/users.module#UsersModule',
    canActivate: [AuthGuardService],
  },
  {
    path: 'profile',
    loadChildren: './features/profile/profile.module#ProfileModule',
    canActivate: [AuthGuardService],
  },
  {
    path: 'auth',
    loadChildren: './features/auth/auth.module#AuthModule',
  },
  {
    path: '**',
    redirectTo: 'auth',
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { onSameUrlNavigation: 'reload', enableTracing: false })],
  exports: [RouterModule],
})
export class AppRoutingModule {}
