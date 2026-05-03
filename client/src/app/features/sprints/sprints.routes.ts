import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./views/sprints-view/sprints-view.component').then((m) => m.SprintsViewComponent),
    pathMatch: 'full',
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./views/sprint-detail-view/sprint-detail-view.component').then((m) => m.SprintDetailViewComponent),
  },
];
