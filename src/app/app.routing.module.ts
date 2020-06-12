import { ShellComponent } from '@app/features/workspaces/shell/shell.component';
import { WorkspaceResolver } from '@core/resolvers/workspace-resolver';
import { NgModule } from '@angular/core';
import { Routes, RouterModule, PreloadAllModules } from '@angular/router';

import { AuthGuardService } from './core/auth/auth-guard.service';

const routes: Routes = [
  {
    path: '',
    redirectTo: '/workspaces',
    canActivate: [AuthGuardService],
    pathMatch: 'full',
  },
  {
    path: 'ws/:workspace',
    resolve: [WorkspaceResolver],
    canActivate: [AuthGuardService],
    component: ShellComponent,
    children: [
      {
        path: '',
        redirectTo: 'projects',
        canActivate: [AuthGuardService],
        pathMatch: 'full',
      },
      {
        path: 'projects',
        loadChildren: () => import('./features/projects/projects.module').then(m => m.ProjectsModule),
        canActivate: [AuthGuardService],
        data: { title: 'Projects' },
      },
      {
        path: 'projects-tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.module').then(m => m.ProjectTasksModule),
        canActivate: [AuthGuardService],
        data: { title: 'Tasks' },
      },
      {
        path: 'tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.module').then(m => m.ProjectTasksModule),
        canActivate: [AuthGuardService],
        runGuardsAndResolvers: 'always',
        data: { title: 'Tasks' },
      },
      {
        path: 'boards',
        loadChildren: () => import('./features/boards/boards.module').then(m => m.BoardsModule),
        canActivate: [AuthGuardService],
        runGuardsAndResolvers: 'always',
        data: { title: 'Boards' },
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.module').then(m => m.UsersModule),
        canActivate: [AuthGuardService],
        data: { title: 'Users' },
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.module').then(m => m.ProfileModule),
        canActivate: [AuthGuardService],
        data: { title: 'Profile' },
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.module').then(m => m.SettingsModule),
        canActivate: [AuthGuardService],
        data: { title: 'Settings' },
      },
      {
        path: '**',
        redirectTo: 'projects',
      },
    ],
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule),
  },
  {
    path: 'workspaces',
    loadChildren: () => import('./features/workspaces/workspaces.module').then(m => m.WorkspacesModule),
    canActivate: [AuthGuardService],
    data: { title: 'Workspaces' },
  },
  {
    path: '**',
    redirectTo: 'auth',
  },
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes, {
      enableTracing: false,
      preloadingStrategy: PreloadAllModules,
    }),
  ],
  exports: [RouterModule],
})
export class AppRoutingModule {}
