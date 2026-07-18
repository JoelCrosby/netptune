import { Routes } from '@angular/router';
import { reportingReadGuard } from './guards/reporting-read.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [reportingReadGuard],
    loadComponent: () =>
      import('./views/reporting-view/reporting-view.component').then(
        (module) => module.ReportingViewComponent
      ),
  },
];
