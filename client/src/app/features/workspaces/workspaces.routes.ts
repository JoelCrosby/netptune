import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '**',
    loadComponent: () =>
      import('@workspaces/views/workspaces-view/workspaces-view.component').then(
        (m) => m.WorkspacesViewComponent
      ),
  },
];
