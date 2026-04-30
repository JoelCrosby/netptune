import { Routes } from '@angular/router';
import { userDetailGuard } from './guards/user-detail.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./views/users-view/users-view.component').then(
        (m) => m.UsersViewComponent
      ),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./views/user-detail-view/user-detail-view.component').then(
        (m) => m.UserDetailViewComponent
      ),
    canActivate: [userDetailGuard],
    data: { back: 'Back to Users' },
  },
];
