import { Routes } from '@angular/router';
import { sprintsReadGuard } from './guards/sprints-read.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./views/sprints-view/sprints-view.component').then(
        (m) => m.SprintsViewComponent
      ),
    pathMatch: 'full',
    canActivate: [sprintsReadGuard],
  },
  {
    path: 'backlog',
    loadComponent: () =>
      import('./views/sprint-backlog-view/sprint-backlog-view.component').then(
        (m) => m.SprintBacklogViewComponent
      ),
    canActivate: [sprintsReadGuard],
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./views/sprint-detail-view/sprint-detail-view.component').then(
        (m) => m.SprintDetailViewComponent
      ),
    canActivate: [sprintsReadGuard],
  },
];
