import { NgModule } from '@angular/core';
import { UsersRoutingModule } from './users-routing.module';
import { SharedModule } from '@app/shared/shared.module';

import { UsersComponent } from './index/users.index.component';
import { UsersService } from './store/users.service';

@NgModule({
  declarations: [UsersComponent],
  imports: [SharedModule, UsersRoutingModule],
  providers: [UsersService],
})
export class UsersModule {}
