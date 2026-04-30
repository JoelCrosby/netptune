import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./views/boards-view/boards-view.component').then(
        (m) => m.BoardsViewComponent
      ),
    pathMatch: 'full',
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./views/board-groups-view/board-groups-view.component').then(
        (m) => m.BoardGroupsViewComponent
      ),
    runGuardsAndResolvers: 'always',
    data: {
      back: 'Back to Boards',
    },
  },
  {
    path: '**',
    loadComponent: () =>
      import('./views/boards-view/boards-view.component').then(
        (m) => m.BoardsViewComponent
      ),
  },
];
