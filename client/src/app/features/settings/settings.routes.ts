import { Routes } from '@angular/router';
import { assistantGuard } from '@core/auth/assistant.guard';
import { PERMISSIONS } from '@core/auth/permissions';
import { workspaceSettingsGuard } from './guards/workspace-settings.guard';

// prettier-ignore

export const routes: Routes = [
  {
    path: 'personal',
    loadComponent: () => import('./views/personal-settings-view/personal-settings-view.component').then((m) => m.PersonalSettingsViewComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'general',
      },
      {
        path: 'general',
        title: $localize`:Page title for general personal settings:General Settings`,
        loadComponent: () => import('./views/personal-general-settings-view/personal-general-settings-view.component').then((m) => m.PersonalGeneralSettingsViewComponent),
      },
      {
        path: 'notifications',
        title: $localize`:Page title for personal notification settings:Notification Settings`,
        loadComponent: () => import('./views/personal-notification-settings-view/personal-notification-settings-view.component').then((m) => m.PersonalNotificationSettingsViewComponent),
      },
      {
        path: 'assistant',
        canActivate: [assistantGuard],
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
        data: { permission: PERMISSIONS.workspace.read },
        title: $localize`:Page title for general workspace settings:General Workspace Settings`,
        loadComponent: () => import('./views/workspace-general-settings-view/workspace-general-settings-view.component').then((m) => m.WorkspaceGeneralSettingsViewComponent),
      },
      {
        path: 'tags',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.tags.read },
        title: $localize`:Page title for workspace tag settings:Workspace Tags`,
        loadComponent: () => import('./views/tags-view/tags-view.component').then((m) => m.TagsViewComponent),
      },
      {
        path: 'tags/:id',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.tags.read, back: $localize`:Link back to the tag list from a single tag:Back to Tags` },
        title: $localize`:Page title for a single workspace tag:Tag`,
        loadComponent: () => import('./views/tag-detail-view/tag-detail-view.component').then((m) => m.TagDetailViewComponent),
      },
      {
        path: 'statuses',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.statuses.read },
        title: $localize`:Page title for workspace task status settings:Workspace Statuses`,
        loadComponent: () => import('./views/statuses-view/statuses-view.component').then((m) => m.StatusesViewComponent),
      },
      {
        path: 'statuses/:id',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.statuses.read, back: $localize`:Link back to the status list from a single status:Back to Statuses` },
        title: $localize`:Page title for a single workspace status:Status`,
        loadComponent: () => import('./views/status-detail-view/status-detail-view.component').then((m) => m.StatusDetailViewComponent),
      },
      {
        path: 'relations',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.relationTypes.read },
        title: $localize`:Page title for workspace task relation type settings:Workspace Relations`,
        loadComponent: () => import('./views/relation-types-view/relation-types-view.component').then((m) => m.RelationTypesViewComponent),
      },
      {
        path: 'relations/:id',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.relationTypes.read, back: $localize`:Link back to the relation type list from a single relation type:Back to Relations` },
        title: $localize`:Page title for a single workspace relation type:Relation Type`,
        loadComponent: () => import('./views/relation-type-detail-view/relation-type-detail-view.component').then((m) => m.RelationTypeDetailViewComponent),
      },
      {
        path: 'service-accounts',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.serviceAccounts.read },
        title: $localize`:Page title for workspace service account settings:Workspace Service Accounts`,
        loadComponent: () => import('./views/service-accounts-view/service-accounts-view.component').then((m) => m.ServiceAccountsViewComponent),
      },
      {
        path: 'data',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.tasks.export },
        title: $localize`:Page title for workspace import and export:Workspace Data`,
        loadComponent: () => import('@app/features/data-transfer/views/data-transfer-view/data-transfer-view.component').then((m) => m.DataTransferViewComponent),
      },
      {
        path: 'data/export',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.tasks.export, back: $localize`:Link back to the data page from the export wizard:Back to Data` },
        title: $localize`:Page title for the guided export builder:Export`,
        loadComponent: () => import('@app/features/data-transfer/views/export-wizard-view/export-wizard-view.component').then((m) => m.ExportWizardViewComponent),
      },
      {
        path: 'data/import',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.tasks.import, back: $localize`:Link back to the data page from the import wizard:Back to Data` },
        title: $localize`:Page title for the guided import builder:Import`,
        loadComponent: () => import('@app/features/data-transfer/views/import-wizard-view/import-wizard-view.component').then((m) => m.ImportWizardViewComponent),
      },
      {
        path: 'data/archive',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.data.importArchive, back: $localize`:Link back to the data page from the archive import:Back to Data` },
        title: $localize`:Page title for the workspace archive import:Import Archive`,
        loadComponent: () => import('@app/features/data-transfer/views/archive-import-view/archive-import-view.component').then((m) => m.ArchiveImportViewComponent),
      },
      {
        path: 'data/import/:sessionId',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.tasks.import, back: $localize`:Link back to the data page from the import wizard:Back to Data` },
        title: $localize`:Page title for resuming a started import:Resume Import`,
        loadComponent: () => import('@app/features/data-transfer/views/import-wizard-view/import-wizard-view.component').then((m) => m.ImportWizardViewComponent),
      },
      {
        path: 'assistant',
        canActivate: [workspaceSettingsGuard],
        data: { permission: PERMISSIONS.assistant.readAllConversations },
        title: $localize`:Page title for workspace assistant conversations:Assistant Conversations`,
        loadComponent: () => import('./views/assistant-conversations-view/assistant-conversations-view.component').then((m) => m.AssistantConversationsViewComponent),
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
