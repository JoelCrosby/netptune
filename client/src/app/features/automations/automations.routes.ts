import { Routes } from '@angular/router';
import { automationsManageGuard } from './guards/automations-manage.guard';
import { automationsReadGuard } from './guards/automations-read.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [automationsReadGuard],
    loadComponent: () =>
      import('./views/automations-view/automations-view.component').then(
        (m) => m.AutomationsViewComponent
      ),
  },
  {
    path: 'new',
    canActivate: [automationsReadGuard, automationsManageGuard],
    loadComponent: () =>
      import('./views/automation-form-view/automation-form-view.component').then(
        (m) => m.AutomationFormViewComponent
      ),
  },
  {
    path: ':id/edit',
    canActivate: [automationsReadGuard, automationsManageGuard],
    loadComponent: () =>
      import('./views/automation-form-view/automation-form-view.component').then(
        (m) => m.AutomationFormViewComponent
      ),
  },
  {
    path: ':id',
    canActivate: [automationsReadGuard],
    loadComponent: () =>
      import('./views/automation-detail-view/automation-detail-view.component').then(
        (m) => m.AutomationDetailViewComponent
      ),
  },
];
