import { NgModule } from '@angular/core';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { ProfileRoutingModule } from './profile-routing.module';
import { ProfileEffects } from './store/profile.effects';
import { profileReducer } from './store/profile.reducer';

@NgModule({
  imports: [
    StoreModule.forFeature('profile', profileReducer),
    EffectsModule.forFeature([ProfileEffects]),
    ProfileRoutingModule,
  ],
  providers: [],
})
export class ProfileModule {}
