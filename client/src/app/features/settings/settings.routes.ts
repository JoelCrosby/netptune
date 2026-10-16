import { Routes } from '@angular/router';
import { assistantGuard } from '@core/auth/assistant.guard';
import { PERMISSIONS } from '@core/auth/permissions';
import { workspaceSettingsGuard } from './guards/workspace-settings.guard';

// prettier-ignore

export const routes: Routes = [
  {
    path: 'personal',
    data: { breadcrumb: $localize`:Breadcrumb for the personal settings section:Personal` },
    loadComponent: () => import('./views/personal-settings-view/personal-settings-view.component').then((m) => m.PersonalSettingsViewComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'general',
      },
      {
        path: 'general',
        data: { breadcrumb: $localize`:Breadcrumb for general personal settings:General` },
        title: $localize`:Page title for general personal settings:General Settings`,
        loadComponent: () => import('./views/personal-general-settings-view/personal-general-settings-view.component').then((m) => m.PersonalGeneralSettingsViewComponent),
      },
      {
        path: 'notifications',
        data: { breadcrumb: $localize`:Breadcrumb for personal notification settings:Notifications` },
        title: $localize`:Page title for personal notification settings:Notification Settings`,
        loadComponent: () => import('./views/personal-notification-settings-view/personal-notification-settings-view.component').then((m) => m.PersonalNotificationSettingsViewComponent),
      },
      {
        path: 'assistant',
        canActivate: [assistantGuard],
        data: { breadcrumb: $localize`:Breadcrumb for the personal assistant key settings:Assistant Keys` },
        title: $localize`:Page title for the personal assistant key settings:Assistant Keys`,
        loadComponent: () => import('./views/personal-assistant-settings-view/personal-assistant-settings-view.component').then((m) => m.PersonalAssistantSettingsViewComponent),
      },
      {
        path: '**',
        redirectTo: '',
      },
    ],
  },
  {
    path: 'workspace',
    data: { breadcrumb: $localize`:Breadcrumb for the workspace settings section:Workspace` },
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
        data: { permission: PERMISSIONS.workspace.read, breadcrumb: $localize`:Breadcrumb for general workspace settings:General` },
        title: $localize`:Page title for general workspace settings:General Workspace Settings`,
        loadComponent: () => import('./views/workspace-general-settings-view/workspace-general-settings-view.component').then((m) => m.WorkspaceGeneralSettingsViewComponent),
      },
      {
        path: 'tags',
        data: { breadcrumb: $localize`:Breadcrumb for workspace tag settings:Tags` },
        children: [
          {
            path: '',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.tags.read },
            title: $localize`:Page title for workspace tag settings:Workspace Tags`,
            loadComponent: () => import('./views/tags-view/tags-view.component').then((m) => m.TagsViewComponent),
          },
          {
            path: ':id',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.tags.read },
            title: $localize`:Page title for a single workspace tag:Tag`,
            loadComponent: () => import('./views/tag-detail-view/tag-detail-view.component').then((m) => m.TagDetailViewComponent),
          },
        ],
      },
      {
        path: 'statuses',
        data: { breadcrumb: $localize`:Breadcrumb for workspace task status settings:Statuses` },
        children: [
          {
            path: '',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.statuses.read },
            title: $localize`:Page title for workspace task status settings:Workspace Statuses`,
            loadComponent: () => import('./views/statuses-view/statuses-view.component').then((m) => m.StatusesViewComponent),
          },
          {
            path: ':id',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.statuses.read },
            title: $localize`:Page title for a single workspace status:Status`,
            loadComponent: () => import('./views/status-detail-view/status-detail-view.component').then((m) => m.StatusDetailViewComponent),
          },
        ],
      },
      {
        path: 'relations',
        data: { breadcrumb: $localize`:Breadcrumb for workspace task relation type settings:Relations` },
        children: [
          {
            path: '',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.relationTypes.read },
            title: $localize`:Page title for workspace task relation type settings:Workspace Relations`,
            loadComponent: () => import('./views/relation-types-view/relation-types-view.component').then((m) => m.RelationTypesViewComponent),
          },
          {
            path: ':id',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.relationTypes.read },
            title: $localize`:Page title for a single workspace relation type:Relation Type`,
            loadComponent: () => import('./views/relation-type-detail-view/relation-type-detail-view.component').then((m) => m.RelationTypeDetailViewComponent),
          },
        ],
      },
      {
        path: 'service-accounts',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.serviceAccounts.read, breadcrumb: $localize`:Breadcrumb for workspace service account settings:Service Accounts` },
        title: $localize`:Page title for workspace service account settings:Workspace Service Accounts`,
        loadComponent: () => import('./views/service-accounts-view/service-accounts-view.component').then((m) => m.ServiceAccountsViewComponent),
      },
      {
        path: 'data',
        data: { breadcrumb: $localize`:Breadcrumb for workspace import and export:Data` },
        children: [
          {
            path: '',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.tasks.export },
            title: $localize`:Page title for workspace import and export:Workspace Data`,
            loadComponent: () => import('@app/features/data-transfer/views/data-transfer-view/data-transfer-view.component').then((m) => m.DataTransferViewComponent),
          },
          {
            path: 'export',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.tasks.export, breadcrumb: $localize`:Breadcrumb for the guided export builder:Export` },
            title: $localize`:Page title for the guided export builder:Export`,
            loadComponent: () => import('@app/features/data-transfer/views/export-wizard-view/export-wizard-view.component').then((m) => m.ExportWizardViewComponent),
          },
          {
            path: 'import',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.tasks.import, breadcrumb: $localize`:Breadcrumb for the guided import builder:Import` },
            title: $localize`:Page title for the guided import builder:Import`,
            loadComponent: () => import('@app/features/data-transfer/views/import-wizard-view/import-wizard-view.component').then((m) => m.ImportWizardViewComponent),
          },
          {
            path: 'archive',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.data.importArchive, breadcrumb: $localize`:Breadcrumb for the workspace archive import:Import Archive` },
            title: $localize`:Page title for the workspace archive import:Import Archive`,
            loadComponent: () => import('@app/features/data-transfer/views/archive-import-view/archive-import-view.component').then((m) => m.ArchiveImportViewComponent),
          },
          {
            path: 'import/:sessionId',
            canActivate: [workspaceSettingsGuard],
            data: { permission: PERMISSIONS.tasks.import, breadcrumb: $localize`:Breadcrumb for resuming a started import:Resume Import` },
            title: $localize`:Page title for resuming a started import:Resume Import`,
            loadComponent: () => import('@app/features/data-transfer/views/import-wizard-view/import-wizard-view.component').then((m) => m.ImportWizardViewComponent),
          },
        ],
      },
      {
        path: 'assistant',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.assistant.readAllConversations, breadcrumb: $localize`:Breadcrumb for the workspace assistant settings:Assistant` },
        title: $localize`:Page title for the workspace assistant settings:Assistant`,
        loadComponent: () => import('./views/workspace-assistant-view/workspace-assistant-view.component').then((m) => m.WorkspaceAssistantViewComponent),
      },
      {
        path: 'source-control',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.sourceControl.read, breadcrumb: $localize`:Breadcrumb for the workspace source control settings:Source Control` },
        title: $localize`:Page title for the workspace source control settings:Source Control`,
        loadComponent: () => import('./views/workspace-source-control-view/workspace-source-control-view.component').then((m) => m.WorkspaceSourceControlViewComponent),
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
