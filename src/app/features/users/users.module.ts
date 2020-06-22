import { NgModule } from '@angular/core';
import { UsersRoutingModule } from './users-routing.module';
import { SharedModule } from '@app/shared/shared.module';

import { UsersListComponent } from './components/users-list/users-list.component';
import { UsersService } from './store/users.service';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { UsersEffects } from './store/users.effects';
import { usersReducer } from './store/users.reducer';
import { StaticModule } from '@app/static/static.module';
import { UsersViewComponent } from './views/users-view/users-view.component';

@NgModule({
  declarations: [UsersListComponent, UsersViewComponent],
  imports: [
    SharedModule,
    StaticModule,
    StoreModule.forFeature('users', usersReducer),
    EffectsModule.forFeature([UsersEffects]),
    UsersRoutingModule,
  ],
  providers: [UsersService],
})
export class UsersModule {}
