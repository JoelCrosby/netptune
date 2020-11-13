import { NgModule } from '@angular/core';
import { PreloadAllModules, RouterModule, Routes } from '@angular/router';
import { ShellComponent } from '@workspaces/components/shell/shell.component';
import { WorkspaceResolver } from '@core/resolvers/workspace-resolver';
import { AuthGuardService } from '@core/auth/auth-guard.service';

// prettier-ignore

const routes: Routes = [
  {
    path: '',
    redirectTo: '/workspaces',
    canActivate: [AuthGuardService],
    pathMatch: 'full',
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule),
    data: { transparentSidebar: true, },
  },
  {
    path: 'workspaces',
    loadChildren: () => import('./features/workspaces/workspaces.module').then((m) => m.WorkspacesModule),
    canActivate: [AuthGuardService],
    data: { title: 'Workspaces', transparentSidebar: true, },
  },
  {
    path: 'profile',
    loadChildren: () => import('./features/profile/profile.module').then((m) => m.ProfileModule),
    canActivate: [AuthGuardService],
    data: { title: 'Profile', transparentSidebar: true, },
  },
  {
    path: ':workspace',
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
        loadChildren: () => import('./features/projects/projects.module').then((m) => m.ProjectsModule),
        canActivate: [AuthGuardService],
        data: { title: 'Projects' },
      },
      {
        path: 'projects-tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.module').then((m) => m.ProjectTasksModule),
        canActivate: [AuthGuardService],
        data: { title: 'Tasks' },
      },
      {
        path: 'tasks',
        loadChildren: () => import('./features/project-tasks/project-tasks.module').then((m) => m.ProjectTasksModule),
        canActivate: [AuthGuardService],
        runGuardsAndResolvers: 'always',
        data: { title: 'Tasks' },
      },
      {
        path: 'boards',
        loadChildren: () => import('./features/boards/boards.module').then((m) => m.BoardsModule),
        canActivate: [AuthGuardService],
        runGuardsAndResolvers: 'always',
        data: { title: 'Boards' },
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.module').then((m) => m.UsersModule),
        canActivate: [AuthGuardService],
        data: { title: 'Users' },
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.module').then((m) => m.SettingsModule),
        canActivate: [AuthGuardService],
        data: { title: 'Settings' },
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
    relativeLinkResolution: 'legacy'
}),
  ],
  exports: [RouterModule],
})
export class AppRoutingModule {}
