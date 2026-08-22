import { Routes } from '@angular/router';
import { taskViewsReadGuard } from './guards/task-views-read.guard';
import { taskViewsWriteGuard } from './guards/task-views-write.guard';

// prettier-ignore

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [taskViewsReadGuard],
    loadComponent: () => import('./views/task-views-view/task-views-view.component').then((m) => m.TaskViewsViewComponent),
  },
  {
    path: 'new',
    canActivate: [taskViewsReadGuard, taskViewsWriteGuard],
    loadComponent: () => import('./views/task-view-form-view/task-view-form-view.component').then((m) => m.TaskViewFormViewComponent),
    data: {
      back: $localize`:Link back to the view list from a single view:Back to Views`,
    },
  },
  {
    path: ':slug/edit',
    canActivate: [taskViewsReadGuard, taskViewsWriteGuard],
    loadComponent: () => import('./views/task-view-form-view/task-view-form-view.component').then((m) => m.TaskViewFormViewComponent),
    data: {
      back: $localize`:Link back to the view list from a single view:Back to Views`,
    },
  },
  {
    path: ':slug',
    canActivate: [taskViewsReadGuard],
    loadComponent: () => import('./views/task-view-detail-view/task-view-detail-view.component').then((m) => m.TaskViewDetailViewComponent),
    data: {
      back: $localize`:Link back to the view list from a single view:Back to Views`,
    },
  },
];
