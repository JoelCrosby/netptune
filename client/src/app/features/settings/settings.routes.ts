import { Routes } from '@angular/router';

// prettier-ignore

export const routes: Routes = [
  {
    path: 'personal',
    loadComponent: () => import('./views/personal-settings-view.component').then((m) => m.PersonalSettingsViewComponent),
  },
  {
    path: 'workspace',
    loadComponent: () => import('./views/workspace-settings-view.component').then((m) => m.WorkspaceSettingsViewComponent),
  },
  {
    path: '**',
    redirectTo: 'personal',
  },
];
