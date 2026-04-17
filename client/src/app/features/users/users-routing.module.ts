import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UsersViewComponent } from './views/users-view/users-view.component';
import { UserDetailViewComponent } from './views/user-detail-view/user-detail-view.component';
import { userDetailGuard } from './guards/user-detail.guard';

const routes: Routes = [
  { path: '', component: UsersViewComponent },
  {
    path: ':id',
    component: UserDetailViewComponent,
    canActivate: [userDetailGuard],
    data: { back: 'Back to Users' },
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class UsersRoutingModule {}
