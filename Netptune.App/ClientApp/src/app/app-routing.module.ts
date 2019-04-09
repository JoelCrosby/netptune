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
    data: { title: 'Projects' },
  },
  {
    path: 'projects-tasks',
    loadChildren: './features/project-tasks/project-tasks.module#ProjectTasksModule',
    canActivate: [AuthGuardService],
    data: { title: 'Tasks' },
  },
  {
    path: 'workspaces',
    loadChildren: './features/workspaces/workspaces.module#WorkspacesModule',
    canActivate: [AuthGuardService],
    data: { title: 'Workspaces' },
  },
  {
    path: 'tasks',
    loadChildren: './features/project-tasks/project-tasks.module#ProjectTasksModule',
    canActivate: [AuthGuardService],
    runGuardsAndResolvers: 'always',
    data: { title: 'Tasks' },
  },
  {
    path: 'users',
    loadChildren: './features/users/users.module#UsersModule',
    canActivate: [AuthGuardService],
    data: { title: 'Users' },
  },
  {
    path: 'profile',
    loadChildren: './features/profile/profile.module#ProfileModule',
    canActivate: [AuthGuardService],
    data: { title: 'Profile' },
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
