import { Routes } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { workspaceSettingsGuard } from './guards/workspace-settings.guard';

// prettier-ignore

export const routes: Routes = [
  {
    path: 'personal',
    loadComponent: () => import('./views/personal-settings-view.component').then((m) => m.PersonalSettingsViewComponent),
  },
  {
    path: 'workspace',
    loadComponent: () => import('./views/workspace-settings-view.component').then((m) => m.WorkspaceSettingsViewComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'general',
      },
      {
        path: 'general',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.workspace.read },
        title: 'General Workspace Settings',
        loadComponent: () => import('./views/workspace-general-settings-view/workspace-general-settings-view.component').then((m) => m.WorkspaceGeneralSettingsViewComponent),
      },
      {
        path: 'tags',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.tags.read },
        title: 'Workspace Tags',
        loadComponent: () => import('./components/tags/tags.component').then((m) => m.TagsComponent),
      },
      {
        path: 'statuses',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.statuses.read },
        title: 'Workspace Statuses',
        loadComponent: () => import('./components/statuses/statuses.component').then((m) => m.StatusesComponent),
      },
      {
        path: 'relations',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.relationTypes.read },
        title: 'Workspace Relations',
        loadComponent: () => import('./components/relation-types/relation-types.component').then((m) => m.RelationTypesComponent),
      },
      {
        path: '**',
        redirectTo: '',
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'personal',
  },
];
