import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '**',
    loadComponent: () =>
      import('./views/project-tasks-view/project-tasks-view.component').then(
        (m) => m.ProjectTasksViewComponent
      ),
  },
];
