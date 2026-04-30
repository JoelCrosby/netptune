import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '**',
    loadComponent: () =>
      import('./views/settings-view/settings-view.component').then(
        (m) => m.SettingsViewComponent
      ),
  },
];
