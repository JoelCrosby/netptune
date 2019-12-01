import { ProfileService } from './store/profile.service';
import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { ProfileComponent } from './index/profile.index.component';
import { ProfileRoutingModule } from './profile-routing.module';
import { ProfileEffects } from './store/profile.effects';
import { profileReducer } from './store/profile.reducer';
import { StaticModule } from '@app/static/static.module';

@NgModule({
  declarations: [ProfileComponent],
  imports: [
    SharedModule,
    StaticModule,
    StoreModule.forFeature('profile', profileReducer),
    EffectsModule.forFeature([ProfileEffects]),
    ProfileRoutingModule,
  ],
  providers: [ProfileService],
})
export class ProfileModule {}
