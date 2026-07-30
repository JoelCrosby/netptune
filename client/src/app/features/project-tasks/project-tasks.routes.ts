import { Routes } from '@angular/router';
import { tasksRestoreGuard } from './guards/tasks-restore.guard';

export const routes: Routes = [
  {
    path: 'archive',
    canActivate: [tasksRestoreGuard],
    loadComponent: () =>
      import('./views/archive-view/archive-view.component').then(
        (m) => m.ArchiveViewComponent
      ),
    data: {
      title: $localize`:Page title for the archived task list:Archive`,
    },
  },
  {
    path: ':systemId',
    loadComponent: () =>
      import('./views/task-detail-page/task-detail-page.component').then(
        (m) => m.TaskDetailPageComponent
      ),
    data: {
      title: $localize`:Page title for a single task detail view:Task Detail`,
      back: $localize`:Link back to the task list from a single task:Back to Tasks`,
    },
  },
  {
    path: '**',
    loadComponent: () =>
      import('./views/project-tasks-view/project-tasks-view.component').then(
        (m) => m.ProjectTasksViewComponent
      ),
  },
];
