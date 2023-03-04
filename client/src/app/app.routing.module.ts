import { NgModule } from '@angular/core';
import { PreloadAllModules, RouterModule, Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth-guard.service';
import { workspaceResovler } from '@core/resolvers/workspace-resolver';
import { ShellComponent } from '@workspaces/components/shell/shell.component';

// prettier-ignore

const routes: Routes = [
  {
    path: '',
    redirectTo: '/workspaces',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule),
    data: { transparentSidebar: true, },
  },
  {
    path: 'workspaces',
    loadChildren: () => import('./features/workspaces/workspaces.module').then((m) => m.WorkspacesModule),
    canActivate: [authGuard],
    data: { title: 'Workspaces', transparentSidebar: true, },
  },
  {
    path: 'profile',
    loadChildren: () => import('./features/profile/profile.module').then((m) => m.ProfileModule),
    canActivate: [authGuard],
    data: { title: 'Profile', transparentSidebar: true, },
  },
  {
    path: ':workspace',
    canActivate: [authGuard],
    resolve: [workspaceResovler],
    component: ShellComponent,
    children: [
      {
        path: '',
        redirectTo: 'projects',
        pathMatch: 'full',
      },
      {
        path: 'projects',
        loadChildren: () => import('./features/projects/projects.module').then((m) => m.ProjectsModule),
        canActivate: [authGuard],
        data: { title: 'Projects' },
      },
      {
        path: 'projects-tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.module').then((m) => m.ProjectTasksModule),
        canActivate: [authGuard],
        data: { title: 'Tasks' },
      },
      {
        path: 'tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.module').then((m) => m.ProjectTasksModule),
        canActivate: [authGuard],
        runGuardsAndResolvers: 'always',
        data: { title: 'Tasks' },
      },
      {
        path: 'boards',
        loadChildren: () => import('./features/boards/boards.module').then((m) => m.BoardsModule),
        canActivate: [authGuard],
        runGuardsAndResolvers: 'always',
        data: { title: 'Boards' },
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.module').then((m) => m.UsersModule),
        canActivate: [authGuard],
        data: { title: 'Users' },
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.module').then((m) => m.SettingsModule),
        canActivate: [authGuard],
        data: { title: 'Settings' },
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.module').then((m) => m.ProfileModule),
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
