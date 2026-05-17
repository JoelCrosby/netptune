import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: ':systemId',
    loadComponent: () =>
      import('./views/task-detail-page/task-detail-page.component').then(
        (m) => m.TaskDetailPageComponent
      ),
    data: { title: 'Task Detail' },
  },
  {
    path: '**',
    loadComponent: () =>
      import('./views/project-tasks-view/project-tasks-view.component').then(
        (m) => m.ProjectTasksViewComponent
      ),
  },
];
