import { Routes } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { workspaceSettingsGuard } from './guards/workspace-settings.guard';

// prettier-ignore

export const routes: Routes = [
  {
    path: 'personal',
    loadComponent: () => import('./views/personal-settings-view/personal-settings-view.component').then((m) => m.PersonalSettingsViewComponent),
  },
  {
    path: 'workspace',
    loadComponent: () => import('./views/workspace-settings-view/workspace-settings-view.component').then((m) => m.WorkspaceSettingsViewComponent),
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
        loadComponent: () => import('./views/tags-view/tags-view.component').then((m) => m.TagsViewComponent),
      },
      {
        path: 'statuses',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.statuses.read },
        title: 'Workspace Statuses',
        loadComponent: () => import('./views/statuses-view/statuses-view.component').then((m) => m.StatusesViewComponent),
      },
      {
        path: 'relations',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.relationTypes.read },
        title: 'Workspace Relations',
        loadComponent: () => import('./views/relation-types-view/relation-types-view.component').then((m) => m.RelationTypesViewComponent),
      },
      {
        path: 'service-accounts',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.serviceAccounts.read },
        title: 'Workspace Service Accounts',
        loadComponent: () => import('./views/service-accounts-view/service-accounts-view.component').then((m) => m.ServiceAccountsViewComponent),
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
