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
    path: ':id',
    loadComponent: () =>
      import('./views/sprint-detail-view/sprint-detail-view.component').then(
        (m) => m.SprintDetailViewComponent
      ),
    canActivate: [sprintsReadGuard],
  },
];
