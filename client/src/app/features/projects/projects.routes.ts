import { projectDetailGuard } from './guards/project-detail.guard';
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./views/projects-view/projects-view.component').then(
        (m) => m.ProjectsViewComponent
      ),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./views/project-detail-view/project-detail-view.component').then(
        (m) => m.ProjectDetailViewComponent
      ),
    canActivate: [projectDetailGuard],
    data: { back: 'Back to Projects' },
  },
];
