import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SettingsRoutingModule } from './settings-routing.module';
import { SettingsIndexComponent } from './index/settings.index.component';
import { SharedModule } from '@app/shared/shared.module';
import { StoreModule } from '@ngrx/store';
import { EffectsModule } from '@ngrx/effects';
import { settingsReducer } from './store/settings.reducer';
import { SettingsEffects } from './store/settings.effects';

@NgModule({
  declarations: [SettingsIndexComponent],
  imports: [
    CommonModule,
    SharedModule,

    StoreModule.forFeature('settings', settingsReducer),
    EffectsModule.forFeature([SettingsEffects]),

    SettingsRoutingModule,
  ],
})
export class SettingsModule {}
