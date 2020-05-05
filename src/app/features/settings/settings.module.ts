import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { SharedModule } from '@app/shared/shared.module';
import { SettingsIndexComponent } from './index/settings.index.component';
import { SettingsRoutingModule } from './settings-routing.module';
import { StaticModule } from '@app/static/static.module';

@NgModule({
  declarations: [SettingsIndexComponent],
  imports: [CommonModule, SharedModule, StaticModule, SettingsRoutingModule],
})
export class SettingsModule {}
