import { ActivatedRoute, Routes } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';
import { tasksPermissionGuard } from './guards/tasks-permission.guard';

export const routes: Routes = [
  {
    path: 'archive',
    canActivate: [tasksPermissionGuard(PERMISSIONS.tasks.restore)],
    loadComponent: () =>
      import('./views/archive-view/archive-view.component').then(
        (m) => m.ArchiveViewComponent
      ),
    data: {
      title: $localize`:Page title for the archived task list:Archive`,
    },
  },
  {
    path: 'new',
    canActivate: [tasksPermissionGuard(PERMISSIONS.tasks.create)],
    loadComponent: () =>
      import('./views/create-task-page/create-task-page.component').then(
        (m) => m.CreateTaskPageComponent
      ),
    data: {
      title: $localize`:Page title for the page that creates a task:Create Task`,
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
      breadcrumb: (route: ActivatedRoute) => route.snapshot.params['systemId'],
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
