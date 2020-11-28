import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { StaticModule } from '@static/static.module';
import { SettingsComponent } from './components/settings/settings.component';
import { TagsComponent } from './components/tags/tags.component';
import { SettingsRoutingModule } from './settings-routing.module';
import { SettingsViewComponent } from './views/settings-view/settings-view.component';
import { TagsInputComponent } from './components/tags-input/tags-input.component';

@NgModule({
  declarations: [SettingsComponent, SettingsViewComponent, TagsComponent, TagsInputComponent],
  imports: [CommonModule, SharedModule, StaticModule, SettingsRoutingModule],
})
export class SettingsModule {}
