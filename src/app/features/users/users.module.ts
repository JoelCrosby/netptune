import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { StaticModule } from '@app/static/static.module';
import { UserCardComponent } from './components/user-card/user-card.component';
import { UsersListComponent } from './components/users-list/users-list.component';
import { UsersRoutingModule } from './users-routing.module';
import { UsersViewComponent } from './views/users-view/users-view.component';

@NgModule({
  declarations: [UsersListComponent, UsersViewComponent, UserCardComponent],
  imports: [SharedModule, StaticModule, UsersRoutingModule],
})
export class UsersModule {}
