import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { SettingsViewComponent } from './views/settings-view/settings-view.component';
import { SettingsRoutingModule } from './settings-routing.module';
import { StaticModule } from '@static/static.module';
import { SettingsComponent } from './components/settings/settings.component';

@NgModule({
  declarations: [SettingsComponent, SettingsViewComponent],
  imports: [CommonModule, SharedModule, StaticModule, SettingsRoutingModule],
})
export class SettingsModule {}
