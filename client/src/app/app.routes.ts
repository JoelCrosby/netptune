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
    data: { title: $localize`:Page title for the workspace picker:Workspaces`, transparentSidebar: true, },
  },
  {
    path: 'source-control/:provider/callback',
    loadComponent: () => import('./features/source-control/views/source-control-callback-view/source-control-callback-view.component').then((m) => m.SourceControlCallbackViewComponent),
    canActivate: [authGuard],
    data: { title: $localize`:Page title while a source control connection completes:Connecting`, transparentSidebar: true, },
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
