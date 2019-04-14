import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';

import { SharedModule } from '@app/shared/shared.module';
import { CoreModule } from '@app/core';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { SettingsModule } from './features/settings/settings.module';
import { StateModule } from './state/state.module';

@NgModule({
  declarations: [AppComponent],
  imports: [
    // angular
    BrowserAnimationsModule,
    BrowserModule,

    // core & shared
    CoreModule,
    StateModule,
    SharedModule,

    // app
    AppRoutingModule,

    SettingsModule,
  ],
  providers: [],
  bootstrap: [AppComponent],
})
export class AppModule {}
