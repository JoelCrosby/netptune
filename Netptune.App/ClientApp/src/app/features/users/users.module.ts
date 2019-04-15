import { NgModule } from '@angular/core';
import { UsersRoutingModule } from './users-routing.module';
import { SharedModule } from '@app/shared/shared.module';

import { UsersComponent } from './index/users.index.component';
import { UsersService } from './store/users.service';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { UsersEffects } from './store/users.effects';
import { usersReducer } from './store/users.reducer';

@NgModule({
  declarations: [UsersComponent],
  imports: [
    SharedModule,
    StoreModule.forFeature('users', usersReducer),
    EffectsModule.forFeature([UsersEffects]),
    UsersRoutingModule,
  ],
  providers: [UsersService],
})
export class UsersModule {}
