import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'new',
    loadComponent: () =>
      import('@workspaces/views/create-workspace-view/create-workspace-view.component').then(
        (m) => m.CreateWorkspaceViewComponent
      ),
    data: {
      title: $localize`:Page title for creating a workspace:Create Workspace`,
    },
  },
  {
    path: '**',
    loadComponent: () =>
      import('@workspaces/views/workspaces-view/workspaces-view.component').then(
        (m) => m.WorkspacesViewComponent
      ),
  },
];
