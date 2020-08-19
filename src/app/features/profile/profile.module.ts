import { ProfileService } from './store/profile.service';
import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { UpdateProfileComponent } from './components/update-profile/update-profile.component';
import { ProfileRoutingModule } from './profile-routing.module';
import { ProfileEffects } from './store/profile.effects';
import { profileReducer } from './store/profile.reducer';
import { StaticModule } from '@app/static/static.module';
import { ProfileViewComponent } from './views/profile-view/profile-view.component';

@NgModule({
  declarations: [UpdateProfileComponent, ProfileViewComponent],
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
