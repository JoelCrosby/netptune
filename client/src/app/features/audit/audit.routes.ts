import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '**',
    loadComponent: () =>
      import('./views/audit-view/audit-view.component').then(
        (m) => m.AuditViewComponent
      ),
  },
];
