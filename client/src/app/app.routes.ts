import { Routes } from '@angular/router';
import { authGuard } from '@core/auth/auth.guard';
import { lastWorkspaceGuard } from '@core/auth/last-workspace.guard';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [lastWorkspaceGuard],
    children: [],
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
    loadChildren: () => import('./workspace.routes').then((m) => m.routes),
  },
  {
    path: '**',
    redirectTo: 'auth',
  },
];
