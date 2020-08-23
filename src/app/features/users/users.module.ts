import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { StaticModule } from '@app/static/static.module';
import { UserListItemComponent } from './components/user-list-item/user-list-item.component';
import { UserListComponent } from './components/user-list/user-list.component';
import { UsersRoutingModule } from './users-routing.module';
import { UsersViewComponent } from './views/users-view/users-view.component';

@NgModule({
  declarations: [UserListComponent, UserListItemComponent, UsersViewComponent],
  imports: [SharedModule, StaticModule, UsersRoutingModule],
})
export class UsersModule {}
