import { Routes } from '@angular/router';
import { userDetailGuard } from './guards/user-detail.guard';
import { UserDetailViewComponent } from './views/user-detail-view/user-detail-view.component';
import { UsersViewComponent } from './views/users-view/users-view.component';

export const routes: Routes = [
  { path: '', component: UsersViewComponent },
  {
    path: ':id',
    component: UserDetailViewComponent,
    canActivate: [userDetailGuard],
    data: { back: 'Back to Users' },
  },
];
