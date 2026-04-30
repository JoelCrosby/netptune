import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '**',
    loadComponent: () =>
      import('./views/profile-view/profile-view.component').then(
        (m) => m.ProfileViewComponent
      ),
  },
];
