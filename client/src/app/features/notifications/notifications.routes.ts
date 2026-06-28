import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./views/notifications-view/notifications-view.component').then(
        (m) => m.NotificationsViewComponent
      ),
    pathMatch: 'full',
    data: { title: 'Notifications' },
  },
];
