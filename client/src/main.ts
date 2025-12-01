import {
  ApplicationRef,
  enableProdMode,
  importProvidersFrom,
  provideZonelessChangeDetection,
} from '@angular/core';
import {
  bootstrapApplication,
  BrowserModule,
  enableDebugTools,
} from '@angular/platform-browser';
import { CoreModule } from '@core/core.module';
import { environment } from '@env/environment';
import { AppComponent } from './app/app.component';
import { AppRoutingModule } from './app/app.routing.module';

if (environment.production) {
  enableProdMode();
}

// Using Profile
// Execute the following in the devtools console:
// > ng.profiler.timeChangeDetection()

const addProfiling = (module: ApplicationRef): void => {
  if (environment.production) return;

  enableDebugTools(module.injector.get(ApplicationRef).components[0]);
};

bootstrapApplication(AppComponent, {
  providers: [
    provideZonelessChangeDetection(),
    importProvidersFrom(
      BrowserModule,
      // core & shared
      CoreModule,
      // app
      AppRoutingModule
    ),
  ],
})
  .then((module) => addProfiling(module))
  .catch((err) => console.log(err));
