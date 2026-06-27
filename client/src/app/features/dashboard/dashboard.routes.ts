import { Routes } from '@angular/router';
import { dashboardReadGuard } from './guards/dashboard-read.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./views/dashboard-view/dashboard-view.component').then(
        (m) => m.DashboardViewComponent
      ),
    pathMatch: 'full',
    canActivate: [dashboardReadGuard],
  },
];
