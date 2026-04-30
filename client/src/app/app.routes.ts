import { Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth.guard';
import { workspaceGuard } from '@core/auth/workspace.guard';
import { workspaceResovler } from '@core/resolvers/workspace-resolver';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/workspaces',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.routes),
    data: { transparentSidebar: true, },
  },
  {
    path: 'workspaces',
    loadChildren: () => import('./features/workspaces/workspaces.routes').then((m) => m.routes),
    canActivate: [authGuard],
    data: { title: 'Workspaces', transparentSidebar: true, },
  },
  {
    path: ':workspace',
    canActivate: [workspaceGuard],
    resolve: [workspaceResovler],
    loadComponent: () => import('./shell/shell.component').then(m => m.ShellComponent),
    children: [
      {
        path: '',
        redirectTo: 'projects',
        pathMatch: 'full',
      },
      {
        path: 'projects',
        loadChildren: () => import('./features/projects/projects.routes').then((m) => m.routes),
        data: { title: 'Projects' },
      },
      {
        path: 'tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: 'Tasks' },
      },
      {
        path: 'boards',
        loadChildren: () => import('./features/boards/boards.routes').then((m) => m.routes),
        runGuardsAndResolvers: 'always',
        data: { title: 'Boards' },
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: 'Users' },
      },
      {
        path: 'audit',
        loadChildren: () => import('./features/audit/audit.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: 'Audit Log' },
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: 'Settings' },
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.routes').then((m) => m.routes),
        canActivate: [authGuard],
        data: { title: 'Profile' },
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'auth',
  },
];
