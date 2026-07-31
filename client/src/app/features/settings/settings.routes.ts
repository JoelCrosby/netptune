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
        title: $localize`:Page title for general workspace settings:General Workspace Settings`,
        loadComponent: () => import('./views/workspace-general-settings-view/workspace-general-settings-view.component').then((m) => m.WorkspaceGeneralSettingsViewComponent),
      },
      {
        path: 'tags',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.tags.read },
        title: $localize`:Page title for workspace tag settings:Workspace Tags`,
        loadComponent: () => import('./views/tags-view/tags-view.component').then((m) => m.TagsViewComponent),
      },
      {
        path: 'tags/:id',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.tags.read, back: $localize`:Link back to the tag list from a single tag:Back to Tags` },
        title: $localize`:Page title for a single workspace tag:Tag`,
        loadComponent: () => import('./views/tag-detail-view/tag-detail-view.component').then((m) => m.TagDetailViewComponent),
      },
      {
        path: 'statuses',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.statuses.read },
        title: $localize`:Page title for workspace task status settings:Workspace Statuses`,
        loadComponent: () => import('./views/statuses-view/statuses-view.component').then((m) => m.StatusesViewComponent),
      },
      {
        path: 'statuses/:id',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.statuses.read, back: $localize`:Link back to the status list from a single status:Back to Statuses` },
        title: $localize`:Page title for a single workspace status:Status`,
        loadComponent: () => import('./views/status-detail-view/status-detail-view.component').then((m) => m.StatusDetailViewComponent),
      },
      {
        path: 'relations',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.relationTypes.read },
        title: $localize`:Page title for workspace task relation type settings:Workspace Relations`,
        loadComponent: () => import('./views/relation-types-view/relation-types-view.component').then((m) => m.RelationTypesViewComponent),
      },
      {
        path: 'relations/:id',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.relationTypes.read, back: $localize`:Link back to the relation type list from a single relation type:Back to Relations` },
        title: $localize`:Page title for a single workspace relation type:Relation Type`,
        loadComponent: () => import('./views/relation-type-detail-view/relation-type-detail-view.component').then((m) => m.RelationTypeDetailViewComponent),
      },
      {
        path: 'service-accounts',
        canActivate: [workspaceSettingsGuard],
        data: { permission: netptunePermissions.serviceAccounts.read },
        title: $localize`:Page title for workspace service account settings:Workspace Service Accounts`,
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
